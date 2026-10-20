using System;
using System.Collections.Concurrent;
using System.Threading;
using Activities.WinUI;
using Xunit;

namespace Activities.WinUI.Tests
{
    public class UiCaptureControllerTests
    {
        private static string RawFor(int x, int y)
        {
            return "/Button[LocalizedControlType=\"кнопка\"][ClassName=\"X\"][Name=\"n\"][AutomationId=\"id\"]" +
                   "[x=" + x + "][y=" + y + "][width=10][height=10][lx=0][ly=0][position()=1][RuntimeId=\"42.1\"]\n";
        }

        private sealed class RecordingProvider : IUiPathProvider
        {
            private readonly Func<int, int, string> _handler;

            public RecordingProvider(Func<int, int, string> handler)
            {
                _handler = handler;
            }

            public void Init() { }
            public void Shutdown() { }
            public string GetUiXPath(int left, int top) { return _handler(left, top); }
        }

        private static void SpinWaitUntil(Func<bool> condition, int timeoutMs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (condition()) return;
                Thread.Sleep(5);
            }
            Assert.True(condition(), "Условие не выполнилось за " + timeoutMs + " мс");
        }

        [Fact]
        public void HoverCapture_RunsProviderOffTheCallingThread()
        {
            int callerThread = Thread.CurrentThread.ManagedThreadId;
            int providerThread = -1;
            var called = new ManualResetEventSlim(false);

            var provider = new RecordingProvider((x, y) =>
            {
                providerThread = Thread.CurrentThread.ManagedThreadId;
                called.Set();
                return RawFor(x, y);
            });

            var controller = new UiCaptureController(provider, a => a());
            controller.BeginSession();
            try
            {
                controller.RequestHoverCapture(5, 6);
                Assert.True(called.Wait(2000), "Провайдер не был вызван");
                Assert.NotEqual(callerThread, providerThread);
            }
            finally
            {
                controller.EndSession();
            }
        }

        [Fact]
        public void HoverCaptured_IsPostedToUiScheduler_AndDeliveredOnlyWhenExecuted()
        {
            var posted = new ConcurrentQueue<Action>();
            var provider = new RecordingProvider((x, y) => RawFor(x, y));
            CapturedElement delivered = null;

            var controller = new UiCaptureController(provider, a => posted.Enqueue(a));
            controller.HoverCaptured += c => delivered = c;

            controller.BeginSession();
            try
            {
                controller.RequestHoverCapture(1, 2);
                SpinWaitUntil(() => !posted.IsEmpty, 2000);
                Assert.Null(delivered);

                Action action;
                Assert.True(posted.TryDequeue(out action));
                action();
                Assert.NotNull(delivered);
            }
            finally
            {
                controller.EndSession();
            }
        }

        [Fact]
        public void EmptyResult_RaisesHoverLostOnUiScheduler()
        {
            var posted = new ConcurrentQueue<Action>();
            var provider = new RecordingProvider((x, y) => string.Empty);
            int lost = 0;

            var controller = new UiCaptureController(provider, a => posted.Enqueue(a));
            controller.HoverLost += () => lost++;

            controller.BeginSession();
            try
            {
                controller.RequestHoverCapture(1, 2);
                SpinWaitUntil(() => !posted.IsEmpty, 2000);
                Assert.Equal(0, lost);

                Action action;
                Assert.True(posted.TryDequeue(out action));
                action();
                Assert.Equal(1, lost);
            }
            finally
            {
                controller.EndSession();
            }
        }

        [Fact]
        public void ElementCapture_RaisesElementCaptured_WithCapturedElement()
        {
            var posted = new ConcurrentQueue<Action>();
            var provider = new RecordingProvider((x, y) => RawFor(x, y));
            CapturedElement delivered = null;

            var controller = new UiCaptureController(provider, a => posted.Enqueue(a));
            controller.ElementCaptured += c => delivered = c;

            controller.BeginSession();
            try
            {
                controller.RequestElementCapture(30, 40);
                SpinWaitUntil(() => !posted.IsEmpty, 2000);

                Action action;
                Assert.True(posted.TryDequeue(out action));
                action();

                Assert.NotNull(delivered);
                Assert.Equal(30, (int)delivered.Bounds.Left);
                Assert.Equal(40, (int)delivered.Bounds.Top);
            }
            finally
            {
                controller.EndSession();
            }
        }

        [Fact]
        public void HoverCapture_OnlyLatestPointIsDelivered()
        {
            var posted = new ConcurrentQueue<Action>();
            var entered = new ManualResetEventSlim(false);
            var release = new ManualResetEventSlim(false);
            var delivered = new ConcurrentBag<CapturedElement>();

            var provider = new RecordingProvider((x, y) =>
            {
                entered.Set();
                release.Wait(2000);
                return RawFor(x, y);
            });

            var controller = new UiCaptureController(provider, a => posted.Enqueue(a));
            controller.HoverCaptured += delivered.Add;

            controller.BeginSession();
            try
            {
                controller.RequestHoverCapture(1, 2);
                Assert.True(entered.Wait(2000), "Первый захват не начался");

                // Пока первый (заведомо устаревший) захват заблокирован, приходят более свежие точки.
                controller.RequestHoverCapture(3, 4);
                controller.RequestHoverCapture(5, 6);
                release.Set();

                SpinWaitUntil(() => !posted.IsEmpty, 3000);

                Action action;
                Assert.True(posted.TryDequeue(out action));
                action();

                Assert.Single(delivered);
                foreach (var captured in delivered)
                {
                    Assert.Equal(5, (int)captured.Bounds.Left);
                    Assert.Equal(6, (int)captured.Bounds.Top);
                }
            }
            finally
            {
                release.Set();
                controller.EndSession();
            }
        }
    }
}
