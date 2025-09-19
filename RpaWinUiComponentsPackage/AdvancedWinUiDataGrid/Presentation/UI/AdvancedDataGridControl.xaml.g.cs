#pragma checksum "D:\www\RB0120APP\NewRpaWinUiComponentsPackage\RpaWinUiComponentsPackage\AdvancedWinUiDataGrid\Presentation\UI\AdvancedDataGridControl.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "123456789ABCDEF"

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Presentation.UI
{
    partial class AdvancedDataGridControl : Microsoft.UI.Xaml.Controls.UserControl
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        private global::Microsoft.UI.Xaml.Controls.Grid MainGrid;
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        private global::Microsoft.UI.Xaml.Controls.ScrollViewer MainScrollViewer;
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        private global::Microsoft.UI.Xaml.Controls.Grid DataGridContainer;
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        private global::Microsoft.UI.Xaml.Controls.ItemsRepeater ColumnHeadersRepeater;
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        private global::Microsoft.UI.Xaml.Controls.ItemsRepeater DataRowsRepeater;
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        private bool _contentLoaded;

        /// <summary>
        /// InitializeComponent()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent()
        {
            if (_contentLoaded)
                return;

            _contentLoaded = true;

            global::System.Uri resourceLocator = new global::System.Uri("ms-appx:///AdvancedWinUiDataGrid/Presentation/UI/AdvancedDataGridControl.xaml");
            global::Microsoft.UI.Xaml.Application.LoadComponent(this, resourceLocator, global::Microsoft.UI.Xaml.Controls.Primitives.ComponentResourceLocation.Application);
        }

        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch (connectionId)
            {
                case 2: // MainGrid
                    this.MainGrid = (global::Microsoft.UI.Xaml.Controls.Grid)target;
                    break;
                case 3: // MainScrollViewer
                    this.MainScrollViewer = (global::Microsoft.UI.Xaml.Controls.ScrollViewer)target;
                    ((global::Microsoft.UI.Xaml.Controls.ScrollViewer)this.MainScrollViewer).PointerWheelChanged += this.OnScrollViewerPointerWheelChanged;
                    break;
                case 4: // DataGridContainer
                    this.DataGridContainer = (global::Microsoft.UI.Xaml.Controls.Grid)target;
                    ((global::Microsoft.UI.Xaml.Controls.Grid)this.DataGridContainer).SizeChanged += this.OnDataGridContainerSizeChanged;
                    break;
                case 5: // ColumnHeadersRepeater
                    this.ColumnHeadersRepeater = (global::Microsoft.UI.Xaml.Controls.ItemsRepeater)target;
                    break;
                case 6: // DataRowsRepeater
                    this.DataRowsRepeater = (global::Microsoft.UI.Xaml.Controls.ItemsRepeater)target;
                    break;
                default:
                    break;
            }
            this._contentLoaded = true;
        }

        /// <summary>
        /// GetBindingConnector(int connectionId, object target)
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Microsoft.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Microsoft.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}