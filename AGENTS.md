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
- UI Automation: только встроенные `UIAutomationClient` / `UIAutomationTypes` (`System.Windows.Automation`), без сторонних библиотек.
- WPF-редактор селекторов (`WinUIExplorer`) запускается из дизайнера активности.

## Сборка и тесты
- Тесты: `dotnet test Activities.WinUI.Tests\Activities.WinUI.Tests.csproj` (SDK-style, net472 + xUnit).
- Полная сборка решения: MSBuild из Visual Studio.
