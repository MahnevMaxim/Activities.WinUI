using System;
using System.Windows.Automation;
using Xunit;

namespace Activities.WinUI.Tests
{
    public class ControlTypeResolverTests
    {
        [Fact]
        public void Resolve_KnownRoles_ReturnsControlType()
        {
            Assert.Same(ControlType.Edit, ControlTypeResolver.Resolve("editable"));
            Assert.Same(ControlType.Edit, ControlTypeResolver.Resolve("edit"));
            Assert.Same(ControlType.Window, ControlTypeResolver.Resolve("window"));
            Assert.Same(ControlType.Button, ControlTypeResolver.Resolve("button"));
            Assert.Same(ControlType.CheckBox, ControlTypeResolver.Resolve("checkbox"));
        }

        [Fact]
        public void Resolve_ProgrammaticName_ReturnsControlType()
        {
            Assert.Same(ControlType.Edit, ControlTypeResolver.Resolve("ControlType.Edit"));
        }

        [Fact]
        public void Resolve_Unknown_Throws()
        {
            Assert.Throws<ArgumentException>(() => ControlTypeResolver.Resolve("doesnotexist"));
        }

        [Fact]
        public void TryResolve_Unknown_ReturnsFalse()
        {
            Assert.False(ControlTypeResolver.TryResolve("doesnotexist", out var controlType));
            Assert.Null(controlType);
        }

        [Fact]
        public void ToRoleName_CommonControlTypes()
        {
            Assert.Equal("editable", ControlTypeResolver.ToRoleName(ControlType.Edit));
            Assert.Equal("window", ControlTypeResolver.ToRoleName(ControlType.Window));
            Assert.Equal("button", ControlTypeResolver.ToRoleName(ControlType.Button));
        }
    }
}
