using System;
using System.Collections.Generic;
using System.Windows.Automation;

namespace Activities.WinUI
{
    /// <summary>Маппинг UiPath-имён ролей (role) на UIA ControlType и обратно.</summary>
    public static class ControlTypeResolver
    {
        private static readonly Dictionary<string, ControlType> ByRole =
            new Dictionary<string, ControlType>(StringComparer.OrdinalIgnoreCase)
            {
                ["window"] = ControlType.Window,
                ["pane"] = ControlType.Pane,
                ["button"] = ControlType.Button,
                ["edit"] = ControlType.Edit,
                ["editable"] = ControlType.Edit,
                ["text"] = ControlType.Text,
                ["checkbox"] = ControlType.CheckBox,
                ["radio"] = ControlType.RadioButton,
                ["radiobutton"] = ControlType.RadioButton,
                ["combobox"] = ControlType.ComboBox,
                ["combo"] = ControlType.ComboBox,
                ["list"] = ControlType.List,
                ["listitem"] = ControlType.ListItem,
                ["menu"] = ControlType.Menu,
                ["menuitem"] = ControlType.MenuItem,
                ["document"] = ControlType.Document,
                ["image"] = ControlType.Image,
                ["table"] = ControlType.Table,
                ["hyperlink"] = ControlType.Hyperlink,
                ["slider"] = ControlType.Slider,
                ["spinner"] = ControlType.Spinner,
                ["tab"] = ControlType.Tab,
                ["tabitem"] = ControlType.TabItem,
                ["toolbar"] = ControlType.ToolBar,
                ["tree"] = ControlType.Tree,
                ["treeitem"] = ControlType.TreeItem,
                ["custom"] = ControlType.Custom,
                ["group"] = ControlType.Group,
                ["statusbar"] = ControlType.StatusBar,
                ["scrollbar"] = ControlType.ScrollBar,
                ["progressbar"] = ControlType.ProgressBar,
                ["separator"] = ControlType.Separator,
                ["datagrid"] = ControlType.DataGrid,
                ["datagriditem"] = ControlType.DataItem,
                ["dataitem"] = ControlType.DataItem,
                ["header"] = ControlType.Header,
                ["headeritem"] = ControlType.HeaderItem,
                ["splitbutton"] = ControlType.SplitButton,
                ["titlebar"] = ControlType.TitleBar,
                ["calendar"] = ControlType.Calendar,
                ["menubar"] = ControlType.MenuBar,
                ["thumb"] = ControlType.Thumb,
                ["tooltip"] = ControlType.ToolTip,
            };

        public static bool TryResolve(string role, out ControlType controlType)
        {
            controlType = null;
            if (string.IsNullOrWhiteSpace(role)) return false;

            if (ByRole.TryGetValue(role, out controlType)) return true;

            // Допускаем программное имя UIA, например "ControlType.Edit".
            var programmatic = role.StartsWith("ControlType.", StringComparison.OrdinalIgnoreCase)
                ? role.Substring("ControlType.".Length)
                : role;

            return ByRole.TryGetValue(programmatic, out controlType);
        }

        public static ControlType Resolve(string role)
        {
            if (!TryResolve(role, out var controlType))
                throw new ArgumentException($"Unknown control type role '{role}'.", nameof(role));
            return controlType;
        }

        public static string ToRoleName(ControlType controlType)
        {
            if (controlType == null) return string.Empty;

            if (controlType == ControlType.Edit) return "editable";
            if (controlType == ControlType.Window) return "window";
            if (controlType == ControlType.Button) return "button";
            if (controlType == ControlType.CheckBox) return "checkbox";
            if (controlType == ControlType.RadioButton) return "radio";
            if (controlType == ControlType.ComboBox) return "combobox";
            if (controlType == ControlType.List) return "list";
            if (controlType == ControlType.ListItem) return "listitem";
            if (controlType == ControlType.Menu) return "menu";
            if (controlType == ControlType.MenuItem) return "menuitem";
            if (controlType == ControlType.Text) return "text";
            if (controlType == ControlType.Document) return "document";
            if (controlType == ControlType.Pane) return "pane";
            if (controlType == ControlType.Tree) return "tree";
            if (controlType == ControlType.TreeItem) return "treeitem";
            if (controlType == ControlType.Hyperlink) return "hyperlink";
            if (controlType == ControlType.Table) return "table";
            if (controlType == ControlType.Calendar) return "calendar";
            if (controlType == ControlType.MenuBar) return "menubar";
            if (controlType == ControlType.Thumb) return "thumb";
            if (controlType == ControlType.ToolTip) return "tooltip";

            return controlType.ProgrammaticName;
        }
    }
}
