using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Runtime;
using WF = System.Windows.Forms; // Псевдоним для WinForms
using SD = System.Drawing; // Псевдоним для System.Drawing

using UndertaleModLib;
using UndertaleModLib.Util;
using UndertaleModLib.Models;

EnsureDataLoaded();

if (Data.IsYYC())
{
    ScriptError("The opened game uses YYC: no code is available.");
    return;
}

UndertaleModTool.MainWindow mainWindow = Application.Current.MainWindow as UndertaleModTool.MainWindow;

ContentControl dataEditor = mainWindow.FindName("DataEditor") as ContentControl;
if (dataEditor is null)
    throw new ScriptException("Can't find \"DataEditor\" control.");

DependencyObject dataEditorChild = VisualTreeHelper.GetChild(dataEditor, 0);
if (dataEditorChild is null)
    throw new ScriptException("Can't find \"DataEditor\" child control.");

public class GlobalVarsViewerForm : WF.Form
{
    private WF.ListBox varsListBox;
    private WF.Button copyButton;
    private WF.Button closeButton;
    private WF.TextBox searchBox;
    private UndertaleData data;
    private List<string> allVariables = new List<string>();

    public GlobalVarsViewerForm(UndertaleData data)
    {
        this.data = data;
        this.Text = "Global Variables Viewer";
        this.Width = 500;
        this.Height = 600;
        this.StartPosition = WF.FormStartPosition.CenterScreen;

        // Search box
        searchBox = new WF.TextBox
        {
            Dock = WF.DockStyle.Top,
            PlaceholderText = "Search variables..."
        };
        searchBox.TextChanged += SearchBox_TextChanged;
        this.Controls.Add(searchBox);

        // ListBox with DoubleBuffered for smooth scrolling
        varsListBox = new WF.ListBox
        {
            Dock = WF.DockStyle.Fill,
            ScrollAlwaysVisible = true,
            SelectionMode = WF.SelectionMode.One,
            Font = new SD.Font("Consolas", 9.75f) // Явное указание через псевдоним
        };
        // Включаем DoubleBuffering для плавного скроллинга
        typeof(WF.ListBox).GetProperty("DoubleBuffered", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance)
            .SetValue(varsListBox, true, null);
        this.Controls.Add(varsListBox);

        // Панель для кнопок внизу
        var buttonPanel = new WF.Panel
        {
            Dock = WF.DockStyle.Bottom,
            Height = 40,
            BackColor = SD.SystemColors.Control // Явное указание через псевдоним
        };
        
        // Кнопка копирования
        copyButton = new WF.Button
        {
            Text = "Copy Selected",
            Width = 120,
            Height = 30,
            Location = new SD.Point(10, 5) // Явное указание через псевдоним
        };
        copyButton.Click += CopyButton_Click;
        buttonPanel.Controls.Add(copyButton);
        
        // Кнопка закрытия
        closeButton = new WF.Button
        {
            Text = "Close",
            Width = 120,
            Height = 30,
            Location = new SD.Point(140, 5) // Явное указание через псевдоним
        };
        closeButton.Click += (s, e) => this.Close();
        buttonPanel.Controls.Add(closeButton);

        this.Controls.Add(buttonPanel);

        // Context menu for copy
        var contextMenu = new WF.ContextMenuStrip();
        var copyMenuItem = new WF.ToolStripMenuItem("Copy");
        copyMenuItem.Click += CopyButton_Click;
        contextMenu.Items.Add(copyMenuItem);
        varsListBox.ContextMenuStrip = contextMenu;

        // Double click handler
        varsListBox.DoubleClick += CopyButton_Click;

        LoadGlobalVariables();
    }

    private void LoadGlobalVariables()
    {
        allVariables.Clear();
        foreach (var variable in data.Variables)
        {
            allVariables.Add(variable.Name.Content);
        }
        UpdateListBox(allVariables);
    }

    private void UpdateListBox(IEnumerable<string> items)
    {
        varsListBox.BeginUpdate();
        varsListBox.Items.Clear();
        foreach (var item in items)
        {
            varsListBox.Items.Add(item);
        }
        varsListBox.EndUpdate();
    }

    private void SearchBox_TextChanged(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(searchBox.Text))
        {
            UpdateListBox(allVariables);
        }
        else
        {
            var filtered = allVariables.Where(v => 
                v.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase));
            UpdateListBox(filtered);
        }
    }

    private void CopyButton_Click(object sender, EventArgs e)
    {
        if (varsListBox.SelectedItem != null)
        {
            WF.Clipboard.SetText(varsListBox.SelectedItem.ToString());
            copyButton.Text = "Copied!";
            Task.Delay(1000).ContinueWith(t => 
            {
                this.Invoke((Action)(() => copyButton.Text = "Copy Selected"));
            });
        }
    }
}

public static void ShowGlobalVars(UndertaleData data)
{
    WF.Application.EnableVisualStyles();
    var form = new GlobalVarsViewerForm(data);
    WF.Application.Run(form);
}

ShowGlobalVars(Data);