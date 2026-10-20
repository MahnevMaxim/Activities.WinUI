using System;
using System.Threading;

namespace Activities.WinUI
{
    /// <summary>
    /// Выполняет захват UI-узлов через IUiPathProvider в фоновом потоке, не блокируя
    /// вызывающий поток (в т.ч. коллбэк low-level хука и UI-поток WPF). Результаты
    /// доставляются в UI-поток через переданный планировщик. Реализует дебаунс
    /// «последний запрос побеждает» (latest-wins): устаревшие результаты отбрасываются.
    /// Подход повторяет RecorderThread из WinAppDriverUIRecorder.
    /// </summary>
    public sealed class UiCaptureController
    {
        private enum RequestKind { Hover, Element }

        private readonly UiElementCapturer _capturer;
        private readonly Action<Action> _postToUiThread;
        private readonly object _gate = new object();

        private Thread _worker;
        private bool _running;

        private bool _hoverPending;
        private int _hoverX;
        private int _hoverY;
        private int _hoverGeneration;

        private bool _clickPending;
        private int _clickX;
        private int _clickY;
        private int _clickGeneration;

        public event Action<CapturedElement> HoverCaptured;
        public event Action HoverLost;
        public event Action<CapturedElement> ElementCaptured;

        public UiCaptureController(IUiPathProvider provider, Action<Action> postToUiThread)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (postToUiThread == null) throw new ArgumentNullException(nameof(postToUiThread));

            _postToUiThread = postToUiThread;
            _capturer = new UiElementCapturer(provider);
        }

        public void BeginSession()
        {
            lock (_gate)
            {
                if (_running) return;
                _running = true;
                _worker = new Thread(WorkerLoop) { IsBackground = true, Name = "UiCaptureController" };
                _worker.Start();
            }
        }

        public void EndSession()
        {
            Thread worker;
            lock (_gate)
            {
                if (!_running) return;
                _running = false;
                _hoverPending = false;
                _clickPending = false;
                Monitor.PulseAll(_gate);
                worker = _worker;
            }

            if (worker != null && worker != Thread.CurrentThread && worker.IsAlive)
                worker.Join(1000);

            lock (_gate)
            {
                _worker = null;
            }
        }

        public void RequestHoverCapture(int x, int y)
        {
            lock (_gate)
            {
                if (!_running) return;
                _hoverX = x;
                _hoverY = y;
                _hoverPending = true;
                _hoverGeneration++;
                Monitor.Pulse(_gate);
            }
        }

        public void RequestElementCapture(int x, int y)
        {
            lock (_gate)
            {
                if (!_running) return;
                _clickX = x;
                _clickY = y;
                _clickPending = true;
                _clickGeneration++;
                Monitor.Pulse(_gate);
            }
        }

        private void WorkerLoop()
        {
            while (true)
            {
                Request request;
                lock (_gate)
                {
                    while (_running && !_clickPending && !_hoverPending)
                        Monitor.Wait(_gate);

                    if (!_running)
                        break;

                    if (_clickPending)
                    {
                        request = new Request(RequestKind.Element, _clickX, _clickY, _clickGeneration);
                        _clickPending = false;
                    }
                    else
                    {
                        request = new Request(RequestKind.Hover, _hoverX, _hoverY, _hoverGeneration);
                        _hoverPending = false;
                    }
                }

                CapturedElement captured = _capturer.Capture(request.X, request.Y);

                bool deliver;
                lock (_gate)
                {
                    deliver = _running && IsStillLatest(request);
                }

                if (deliver)
                {
                    RequestKind kind = request.Kind;
                    _postToUiThread(() => Deliver(kind, captured));
                }
            }
        }

        private bool IsStillLatest(Request request)
        {
            if (request.Kind == RequestKind.Hover)
                return request.Generation == _hoverGeneration;
            return request.Generation == _clickGeneration;
        }

        private void Deliver(RequestKind kind, CapturedElement captured)
        {
            if (kind == RequestKind.Hover)
            {
                if (captured != null)
                    HoverCaptured?.Invoke(captured);
                else
                    HoverLost?.Invoke();
            }
            else
            {
                ElementCaptured?.Invoke(captured);
            }
        }

        private struct Request
        {
            public readonly RequestKind Kind;
            public readonly int X;
            public readonly int Y;
            public readonly int Generation;

            public Request(RequestKind kind, int x, int y, int generation)
            {
                Kind = kind;
                X = x;
                Y = y;
                Generation = generation;
            }
        }
    }
}
