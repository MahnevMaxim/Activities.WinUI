# Правила разработки (Activities.WinUI)

## TDD — обязательное правило
- Любая разработка идёт только через тестирование (red → green → refactor).
- Сначала пишется падающий тест, затем минимальная реализация, затем рефакторинг.
- Каждая новая возможность должна быть покрыта юнит-тестами в проекте `Activities.WinUI.Tests`.
- Перед завершением задачи все тесты обязаны проходить (`dotnet test`).

## Соглашения проекта
- Целевая платформа: .NET Framework 4.7.2.
- Формат селектора: UiPath-совместимый XML (`<wnd .../><ctrl .../>`).
- Активности WF: только синхронные `CodeActivity` / `CodeActivity<T>` (без `AsyncCodeActivity`).
- UI Automation: встроенные `UIAutomationClient` / `UIAutomationTypes` (`System.Windows.Automation`)
  + нативный `UIXPathLib.dll` (C++) через P/Invoke.
- Захват элементов — по образцу WinAppDriverUIRecorder:
  - глобальные low-level хуки `WH_MOUSE_LL` / `WH_KEYBOARD_LL`;
  - отдельный поток записи + дебаунс + порог сдвига курсора;
  - обход raw-дерева UIA (`RawViewWalker`, `FindAll(Children)`, `CompareElements`);
  - `ElementFromPoint` → цель, при пустом rect — fallback `ElementContainingPoint`;
  - стратегия по классу корневого окна: `Chrome_WidgetWin`/`MozillaWindowClass` → «цель→корень»;
    `Progman`/`IEFrame`/`SysListView32` → от верхнего окна; иначе → от корня рабочего стола;
  - атрибуты узла: роль (Tag), имя, automation id, класс, координаты, position(), runtime id.
- Нативный `UIXPathLib.dll` поставляется как бинарник в проекте; доступ к нему изолирован
  за интерфейсом `IUiPathProvider`, чтобы позже заменить на управляемую реализацию без
  изменения вызывающего кода.
- Логирование: NLog в файл (UIRecorder и WinUIExplorer).
- DPI awareness: `SetProcessDPIAware()` на старте процесса захвата.
- WPF-редактор селекторов (`WinUIExplorer`) запускается из дизайнера активности.

## Источник логики и фич (портирование)
- Эталонный проект: `C:\Users\Maxxx\source\repos\WinAppDriver\Tools\UIRecorder`
  (WinAppDriverUIRecorder, Microsoft, 2018, лицензия MIT — при переносе кода сохранять
  копирайт/указание лицензии).
- Логику и фичи захвата/построения селектора копируем именно оттуда, а не пишем «с нуля».
- Соответствие файлов эталона нашему коду:
  - `UIRecorder\MouseKeyboardHook.cs` → low-level хуки (`WH_MOUSE_LL`/`WH_KEYBOARD_LL`, `HookProc`);
  - `UIRecorder\MouseKeyboardEventHandler.cs` → отдельный поток записи + дебаунс-таймер + `RecorderThread`;
  - `UIRecorder\Win32API.cs` → P/Invoke (`GetUiXPath`, `InitUiTreeWalk`, `GetPhysicalCursorPos`, `PostMessage`, `BUFFERSIZE=16384`);
  - `UIRecorder\XmlNodePathRecorder.cs` / `GenerateXPath.cs` → разбор узлов и построение селектора;
  - `UIRecorder\UiTreeNode.cs` → модель узла дерева (`UiaPathNode`).
- Правило переноса хуков: low-level hook-коллбэки обязаны быть лёгкими и возвращаться быстро;
  тяжёлый обход UIA (`GetUiXPath`) выполняется ТОЛЬКО в фоновом потоке, никогда в UI-потоке или
  в коллбэке хука (иначе фриз UI и молчаливое снятие хука системой по `LowLevelHooksTimeout`).

## Сборка и тесты
- Тесты: `dotnet test Activities.WinUI.Tests\Activities.WinUI.Tests.csproj` (SDK-style, net472 + xUnit).
- Полная сборка решения: MSBuild из Visual Studio.
