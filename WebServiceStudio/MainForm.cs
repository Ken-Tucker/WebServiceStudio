using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace WebServiceStudio
{
    public class MainForm : Form
    {
        private const string localHost = "https://localhost";
        private static bool isV1;
        private static MainForm mainForm;

        private static string MiniHelpText =
            "\r\n        .NET Webservice Studio is a tool to invoke webmethods interactively. The user can provide a WSDL endpoint. On clicking button Get the tool fetches the WSDL, generates .NET proxy from the WSDL and displays the list of methods available. The user can choose any method and provide the required input parameters. On clicking Invoke the SOAP request is sent to the server and the response is parsed to display the return value.\r\n        ";

        private readonly Container components = null;

        private Button buttonBrowseFile;
        private Button buttonGet;
        private Button buttonInvoke;
        private Button buttonSend;
        private RichTextBoxFinds findOption = RichTextBoxFinds.None;
        private Label labelEndPointUrl;
        private Label labelInput;
        private Label labelInputValue;
        private Label labelOutput;
        private Label labelOutputValue;
        private Label labelRequest;
        private Label labelResponse;
        private MenuStrip mainMenu1;
        private ToolStripMenuItem menuItem1;
        private ToolStripMenuItem menuItem2;
        private ToolStripMenuItem menuItem3;
        private ToolStripMenuItem menuItemAbout;
        private ToolStripMenuItem menuItemExit;
        private ToolStripMenuItem menuItemFind;
        private ToolStripMenuItem menuItemFindNext;
        private ToolStripMenuItem menuItemHelp;
        private ToolStripMenuItem menuItemOptions;
        private ToolStripMenuItem menuItemSaveAll;
        private ToolStripMenuItem menuItemTreeInputCopy;
        private ToolStripMenuItem menuItemTreeInputPaste;
        private ToolStripMenuItem menuItemTreeOutputCopy;

        private OpenFileDialog openWsdlDialog;
        private Panel panelBottomMain;
        private Panel panelLeftInvoke;
        private Panel panelLeftRaw;
        private Panel panelLeftWsdl;
        private Panel panelRightInvoke;
        private Panel panelRightRaw;
        private Panel panelRightWsdl;
        private Panel panelTopMain;
        private PropertyGrid propInput;
        private PropertyGrid propOutput;
        private PropertyGrid propRequest;
        private RichTextBox richMessage;
        private RichTextBox richRequest;
        private RichTextBox richResponse;
        private RichTextBox richWsdl;
        private SaveFileDialog saveAllDialog;
        private string searchStr = "";
        private Splitter splitterInvoke;
        private Splitter splitterRaw;
        private Splitter splitterWsdl;
        private TabControl tabMain;
        private TabPage tabPageInvoke;
        private TabPage tabPageMessage;
        private TabPage tabPageRaw;
        private TabPage tabPageWsdl;
        private ComboBox textEndPointUri;
        private ToolStripButton toolBarButton1;
        private TreeView treeInput;
        private TreeView treeMethods;
        private TreeView treeOutput;
        private TreeView treeWsdl;
        private Wsdl wsdl;
        private IList<SoapParameter> requestParameters = new List<SoapParameter>();

        public MainForm()
        {
            InitializeComponent();
            wsdl = new Wsdl();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void buttonBrowseFile_Click(object sender, EventArgs e)
        {
            if (openWsdlDialog.ShowDialog() == DialogResult.OK)
            {
                textEndPointUri.Text = openWsdlDialog.FileName;
            }
        }

        private void buttonGet_Click(object sender, EventArgs e)
        {
            if (buttonGet.Text == "Get")
            {
                ClearAllTabs();
                tabMain.SelectedTab = tabPageMessage;
                string text = textEndPointUri.Text;
                wsdl.Reset();
                wsdl.Paths.Add(text);
                new Thread(wsdl.Generate).Start();
                buttonGet.Text = "Cancel";
            }
            else
            {
                buttonGet.Text = "Get";
                ShowMessageInternal(this, MessageType.Failure, "Cancelled");
                wsdl.Reset();
                wsdl = new Wsdl();
            }
        }

        private void buttonInvoke_Click(object sender, EventArgs e)
        {
            Cursor cursor = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                InvokeWebMethod();
            }
            finally
            {
                Cursor = cursor;
            }
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            SendWebRequest();
        }

        private void ClearAllTabs()
        {
            richWsdl.Clear();
            richWsdl.Font = Configuration.MasterConfig.UiSettings.WsdlFont;
            treeWsdl.Nodes.Clear();
            richMessage.Clear();
            richMessage.Font = Configuration.MasterConfig.UiSettings.MessageFont;
            richRequest.Clear();
            richRequest.Font = Configuration.MasterConfig.UiSettings.ReqRespFont;
            richResponse.Clear();
            richResponse.Font = Configuration.MasterConfig.UiSettings.ReqRespFont;
            treeMethods.Nodes.Clear();
            treeInput.Nodes.Clear();
            treeOutput.Nodes.Clear();
            propInput.SelectedObject = null;
            propOutput.SelectedObject = null;
        }

        private void CopyToClipboard(TreeNodeProperty tnp)
        {
            if (!IsValidCopyNode(tnp))
            {
                throw new Exception("Cannot copy from here");
            }
            object o = tnp.ReadChildren();
            if (o != null)
            {
                var writer = new StringWriter();
                Type[] extraTypes = { o.GetType() };
                Type type = (o is DataSet) ? typeof(DataSet) : typeof(object);
                new XmlSerializer(type, extraTypes).Serialize(writer, o);
                Clipboard.SetDataObject(writer.ToString());
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void FillInvokeTab()
        {
            treeMethods.Nodes.Clear();
            foreach (ServiceOperation operation in wsdl.Operations)
            {
                treeMethods.Nodes.Add(operation.ToString()).Tag = operation;
            }
            treeMethods.ExpandAll();
        }

        private void FillWsdlTab()
        {
            if ((wsdl.Wsdls != null) && (wsdl.Wsdls.Count != 0))
            {
                int num3;
                richWsdl.Text = wsdl.Wsdls[0];
                treeWsdl.Nodes.Clear();
                TreeNode node = treeWsdl.Nodes.Add("WSDLs");
                var writer = new XmlTreeWriter();
                for (int i = 0; i < wsdl.Wsdls.Count; i++)
                {
                    num3 = i + 1;
                    TreeNode root = node.Nodes.Add("WSDL#" + num3);
                    root.Tag = wsdl.Wsdls[i];
                    writer.FillTree(wsdl.Wsdls[i], root);
                }
                TreeNode node3 = treeWsdl.Nodes.Add("Schemas");
                for (int j = 0; j < wsdl.Xsds.Count; j++)
                {
                    num3 = j + 1;
                    TreeNode node4 = node3.Nodes.Add("Schema#" + num3);
                    node4.Tag = wsdl.Xsds[j];
                    writer.FillTree(wsdl.Xsds[j], node4);
                }
                node.Expand();
            }
        }

        private void Find()
        {
            tabMain.SelectedTab = tabPageWsdl;
            richWsdl.Find(searchStr, richWsdl.SelectionStart + richWsdl.SelectionLength, findOption);
        }

        private void InitializeComponent()
        {
            textEndPointUri = new ComboBox();
            buttonGet = new Button();
            labelEndPointUrl = new Label();
            mainMenu1 = new MenuStrip();
            menuItem1 = new ToolStripMenuItem();
            menuItemSaveAll = new ToolStripMenuItem();
            menuItemExit = new ToolStripMenuItem();
            menuItem2 = new ToolStripMenuItem();
            menuItemTreeOutputCopy = new ToolStripMenuItem();
            menuItemTreeInputCopy = new ToolStripMenuItem();
            menuItemTreeInputPaste = new ToolStripMenuItem();
            menuItemFind = new ToolStripMenuItem();
            menuItemFindNext = new ToolStripMenuItem();
            menuItemOptions = new ToolStripMenuItem();
            menuItem3 = new ToolStripMenuItem();
            menuItemAbout = new ToolStripMenuItem();
            menuItemHelp = new ToolStripMenuItem();
            openWsdlDialog = new OpenFileDialog();
            toolBarButton1 = new ToolStripButton();
            buttonBrowseFile = new Button();
            saveAllDialog = new SaveFileDialog();
            tabPageInvoke = new TabPage();
            panelLeftInvoke = new Panel();
            panelRightInvoke = new Panel();
            splitterInvoke = new Splitter();
            propOutput = new PropertyGrid();
            propInput = new PropertyGrid();
            labelOutputValue = new Label();
            labelInputValue = new Label();
            treeMethods = new TreeView();
            labelOutput = new Label();
            labelInput = new Label();
            treeOutput = new TreeView();
            buttonInvoke = new Button();
            treeInput = new TreeView();
            tabPageWsdl = new TabPage();
            panelLeftWsdl = new Panel();
            panelRightWsdl = new Panel();
            splitterWsdl = new Splitter();
            treeWsdl = new TreeView();
            richWsdl = new RichTextBox();
            tabPageMessage = new TabPage();
            richMessage = new RichTextBox();
            tabPageRaw = new TabPage();
            panelLeftRaw = new Panel();
            panelRightRaw = new Panel();
            splitterRaw = new Splitter();
            buttonSend = new Button();
            richRequest = new RichTextBox();
            propRequest = new PropertyGrid();
            richResponse = new RichTextBox();
            labelRequest = new Label();
            labelResponse = new Label();
            tabMain = new TabControl();
            panelTopMain = new Panel();
            panelBottomMain = new Panel();
            tabPageInvoke.SuspendLayout();
            panelLeftInvoke.SuspendLayout();
            panelRightInvoke.SuspendLayout();
            tabPageWsdl.SuspendLayout();
            panelLeftWsdl.SuspendLayout();
            panelRightWsdl.SuspendLayout();
            tabPageMessage.SuspendLayout();
            tabPageRaw.SuspendLayout();
            panelLeftRaw.SuspendLayout();
            panelRightRaw.SuspendLayout();
            tabMain.SuspendLayout();
            panelTopMain.SuspendLayout();
            panelBottomMain.SuspendLayout();
            base.SuspendLayout();
            textEndPointUri.Location = new Point(0x58, 0x10);
            textEndPointUri.Name = "textEndPointUri";
            textEndPointUri.Size = new Size(0x1bc, 20);
            textEndPointUri.DropDownStyle = ComboBoxStyle.DropDown;
            textEndPointUri.Items.AddRange(Configuration.MasterConfig.InvokeSettings.RecentlyUsedUris);
            if (textEndPointUri.Items.Count > 0)
            {
                textEndPointUri.SelectedIndex = 0;
            }
            else
            {
                textEndPointUri.Text = "";
            }
            textEndPointUri.KeyPress += textEndPointUri_KeyPress;
            buttonGet.Location = new Point(0x298, 12);
            buttonGet.Name = "buttonGet";
            buttonGet.FlatStyle = FlatStyle.Popup;
            buttonGet.Size = new Size(60, 0x18);
            buttonGet.Text = "Get";
            buttonGet.Click += buttonGet_Click;
            labelEndPointUrl.Location = new Point(0, 0x10);
            labelEndPointUrl.Name = "labelEndPointUrl";
            labelEndPointUrl.Size = new Size(0x58, 0x18);
            labelEndPointUrl.Text = "WSDL EndPoint";
            mainMenu1.Items.AddRange(new[] { menuItem1, menuItem2, menuItem3 });
            menuItem1.DropDownItems.AddRange(new[] { menuItemSaveAll, menuItemExit });
            menuItem1.Text = "File";
            menuItemSaveAll.Text = "Save All Files...";
            menuItemSaveAll.Click += menuItemSaveAll_Click;
            menuItemExit.Text = "Exit";
            menuItemExit.Click += menuItemExit_Click;
            menuItem2.DropDownItems.AddRange(new[] { menuItemFind, menuItemFindNext, menuItemOptions });
            menuItem2.Text = "Edit";
            menuItemFind.ShortcutKeys = Keys.Control | Keys.F;
            menuItemFind.Text = "Find...";
            menuItemFind.Click += menuItemFind_Click;
            menuItemFindNext.ShortcutKeys = Keys.F3;
            menuItemFindNext.Text = "Find Next";
            menuItemFindNext.Click += menuItemFindNext_Click;
            menuItemOptions.Text = "Options...";
            menuItemOptions.Click += menuItemOptions_Click;
            menuItem3.DropDownItems.AddRange(new[] { menuItemHelp, menuItemAbout });
            menuItem3.Text = "Help";
            menuItemAbout.Text = "About...";
            menuItemAbout.Click += menuItemAbout_Click;
            menuItemHelp.Text = "Help";
            menuItemHelp.Click += menuItemHelp_Click;
            try
            {
                openWsdlDialog.DefaultExt = "wsdl";
                openWsdlDialog.Multiselect = true;
                openWsdlDialog.Title = "Open WSDL";
                openWsdlDialog.CheckFileExists = false;
                openWsdlDialog.CheckPathExists = false;
                saveAllDialog.FileName = "doc1";
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
            toolBarButton1.Text = "Open Wsdl...";
            toolBarButton1.ToolTipText = "Open WSDL file(s)";
            buttonBrowseFile.Location = new Point(540, 12);
            buttonBrowseFile.FlatStyle = FlatStyle.Popup;
            buttonBrowseFile.Name = "buttonBrowseFile";
            buttonBrowseFile.Size = new Size(0x74, 0x18);
            buttonBrowseFile.Text = "Browse Wsdl ...";
            buttonBrowseFile.TextAlign = ContentAlignment.TopCenter;
            buttonBrowseFile.Click += buttonBrowseFile_Click;
            tabPageInvoke.Controls.AddRange(new Control[] { splitterInvoke, panelRightInvoke, panelLeftInvoke });
            tabPageInvoke.Name = "tabPageInvoke";
            tabPageInvoke.Tag = "";
            tabPageInvoke.Text = "Invoke";
            panelLeftInvoke.BorderStyle = BorderStyle.None;
            panelLeftInvoke.Controls.AddRange(new Control[] { treeMethods });
            panelLeftInvoke.Dock = DockStyle.Left;
            panelLeftInvoke.Name = "panelLeftInvoke";
            panelLeftInvoke.Size = new Size(0xd0, 0x1fd);
            panelRightInvoke.BorderStyle = BorderStyle.None;
            panelRightInvoke.Controls.AddRange(new Control[]
            {
                labelOutputValue, labelInputValue, labelOutput, labelInput, treeInput, treeOutput, propOutput, propInput,
                buttonInvoke
            });
            panelRightInvoke.Dock = DockStyle.Fill;
            panelRightInvoke.Name = "panelRightInvoke";
            panelRightInvoke.Location = new Point(0xd0, 0);
            panelRightInvoke.Size = new Size(0x228, 0x1fd);
            panelRightInvoke.SizeChanged += PanelRightInvoke_SizeChanged;
            splitterInvoke.Location = new Point(0xd0, 0);
            splitterInvoke.Name = "splitterInvoke";
            splitterInvoke.Size = new Size(3, 0x1fd);
            splitterInvoke.TabStop = false;
            propOutput.CommandsVisibleIfAvailable = true;
            propOutput.HelpVisible = false;
            propOutput.LargeButtons = false;
            propOutput.LineColor = SystemColors.ScrollBar;
            propOutput.Location = new Point(0x110, 0x158);
            propOutput.Name = "propOutput";
            propOutput.PropertySort = PropertySort.NoSort;
            propOutput.Size = new Size(0xe8, 0x110);
            propOutput.Text = "propOutput";
            propOutput.ToolbarVisible = false;
            propOutput.ViewBackColor = SystemColors.Window;
            propOutput.ViewForeColor = SystemColors.WindowText;
            propInput.CommandsVisibleIfAvailable = true;
            propInput.HelpVisible = false;
            propInput.LargeButtons = false;
            propInput.LineColor = SystemColors.ScrollBar;
            propInput.Location = new Point(0x110, 0x18);
            propInput.Name = "propInput";
            propInput.PropertySort = PropertySort.NoSort;
            propInput.Size = new Size(0xe8, 0x110);
            propInput.Text = "propInput";
            propInput.ToolbarVisible = false;
            propInput.ViewBackColor = SystemColors.Window;
            propInput.ViewForeColor = SystemColors.WindowText;
            treeMethods.HideSelection = false;
            treeMethods.ImageIndex = -1;
            treeMethods.Dock = DockStyle.Fill;
            treeMethods.Name = "treeMethods";
            treeMethods.SelectedImageIndex = -1;
            treeMethods.AfterSelect += treeMethods_AfterSelect;
            labelInputValue.Location = new Point(0x110, 8);
            labelInputValue.Name = "labelInputValue";
            labelInputValue.Size = new Size(0x38, 0x10);
            labelInputValue.Text = "Value";
            labelOutputValue.Location = new Point(0x110, 320);
            labelOutputValue.Name = "labelOutputValue";
            labelOutputValue.Size = new Size(0x38, 0x10);
            labelOutputValue.Text = "Value";
            labelOutput.Location = new Point(8, 320);
            labelOutput.Name = "labelOutput";
            labelOutput.Size = new Size(0x40, 0x10);
            labelOutput.Text = "Output";
            labelInput.Location = new Point(8, 8);
            labelInput.Name = "labelInput";
            labelInput.Size = new Size(0x70, 0x10);
            labelInput.Text = "Input";
            treeOutput.ImageIndex = -1;
            treeOutput.Location = new Point(8, 0x158);
            treeOutput.Name = "treeOutput";
            treeOutput.SelectedImageIndex = -1;
            treeOutput.Size = new Size(0x100, 0x110);
            treeOutput.AfterSelect += treeOutput_AfterSelect;
            treeOutput.ContextMenuStrip = new ContextMenuStrip();
            treeOutput.ContextMenuStrip.Items.Add(menuItemTreeOutputCopy);
            menuItemTreeOutputCopy.ShortcutKeys = Keys.Control | Keys.C;
            menuItemTreeOutputCopy.Text = "Copy";
            menuItemTreeOutputCopy.Click += treeOutputMenuCopy_Click;
            buttonInvoke.Location = new Point(0x1c8, 0x138);
            buttonInvoke.Name = "buttonInvoke";
            buttonInvoke.FlatStyle = FlatStyle.Popup;
            buttonInvoke.Size = new Size(0x38, 0x18);
            buttonInvoke.Text = "Invoke";
            buttonInvoke.Click += buttonInvoke_Click;
            treeInput.HideSelection = false;
            treeInput.ImageIndex = -1;
            treeInput.Location = new Point(8, 0x18);
            treeInput.Name = "treeInput";
            treeInput.SelectedImageIndex = -1;
            treeInput.Size = new Size(0x100, 0x110);
            treeInput.AfterSelect += treeInput_AfterSelect;
            treeInput.ContextMenuStrip = new ContextMenuStrip();
            treeInput.ContextMenuStrip.Items.Add(menuItemTreeInputCopy);
            treeInput.ContextMenuStrip.Items.Add(menuItemTreeInputPaste);
            menuItemTreeInputCopy.ShortcutKeys = Keys.Control | Keys.C;
            menuItemTreeInputCopy.Text = "Copy";
            menuItemTreeInputCopy.Text = "Copy";
            menuItemTreeInputCopy.Click += treeInputMenuCopy_Click;
            menuItemTreeInputPaste.ShortcutKeys = Keys.Control | Keys.V;
            menuItemTreeInputPaste.Text = "Paste";
            menuItemTreeInputPaste.Click += treeInputMenuPaste_Click;
            tabPageWsdl.Controls.AddRange(new Control[] { splitterWsdl, panelRightWsdl, panelLeftWsdl });
            tabPageWsdl.Name = "tabPageWsdl";
            tabPageWsdl.Tag = "";
            tabPageWsdl.Text = "WSDLs & Proxy";
            panelLeftWsdl.BorderStyle = BorderStyle.None;
            panelLeftWsdl.Controls.AddRange(new Control[] { treeWsdl });
            panelLeftWsdl.Dock = DockStyle.Left;
            panelLeftWsdl.Name = "panelLeftWsdl";
            panelLeftWsdl.Size = new Size(0xd0, 0x1fd);
            panelRightWsdl.BorderStyle = BorderStyle.None;
            panelRightWsdl.Controls.AddRange(new Control[] { richWsdl });
            panelRightWsdl.Dock = DockStyle.Fill;
            panelRightWsdl.Name = "panelRightWsdl";
            panelRightWsdl.Location = new Point(0xd0, 0);
            panelRightWsdl.Size = new Size(0x228, 0x1fd);
            splitterWsdl.Location = new Point(0xd0, 0);
            splitterWsdl.Name = "splitterWsdl";
            splitterWsdl.Size = new Size(3, 0x1fd);
            splitterWsdl.TabStop = false;
            treeWsdl.ImageIndex = -1;
            treeWsdl.Dock = DockStyle.Fill;
            treeWsdl.Name = "treeWsdl";
            treeWsdl.SelectedImageIndex = -1;
            treeWsdl.AfterSelect += treeWsdl_AfterSelect;
            richWsdl.Font = Configuration.MasterConfig.UiSettings.WsdlFont;
            richWsdl.Dock = DockStyle.Fill;
            richWsdl.Name = "richWsdl";
            richWsdl.ReadOnly = true;
            richWsdl.Text = "";
            richWsdl.WordWrap = false;
            richWsdl.HideSelection = false;
            tabPageMessage.Controls.AddRange(new Control[] { richMessage });
            tabPageMessage.Name = "tabPageMessage";
            tabPageMessage.Tag = "";
            tabPageMessage.Text = "Messages";
            richMessage.Font = Configuration.MasterConfig.UiSettings.MessageFont;
            richMessage.Dock = DockStyle.Fill;
            richMessage.Name = "richMessage";
            richMessage.ReadOnly = true;
            richMessage.Text = "";
            tabMain.Controls.AddRange(new Control[] { tabPageInvoke, tabPageRaw, tabPageWsdl, tabPageMessage });
            tabMain.Dock = DockStyle.Fill;
            tabMain.Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            tabMain.ItemSize = new Size(0x2a, 0x12);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Appearance = TabAppearance.FlatButtons;
            tabMain.SelectedIndexChanged += tabMain_SelectedIndexChanged;
            tabPageRaw.Controls.AddRange(new Control[] { splitterRaw, panelRightRaw, panelLeftRaw });
            tabPageRaw.Name = "tabPageRaw";
            tabPageRaw.Text = "Request/Response";
            panelLeftRaw.BorderStyle = BorderStyle.None;
            panelLeftRaw.Controls.AddRange(new Control[] { propRequest });
            panelLeftRaw.Dock = DockStyle.Left;
            panelLeftRaw.Name = "panelLeftRaw";
            panelLeftRaw.Size = new Size(0xd0, 0x1fd);
            panelLeftRaw.SizeChanged += PanelLeftRaw_SizeChanged;
            panelRightRaw.BorderStyle = BorderStyle.None;
            panelRightRaw.Controls.AddRange(new Control[]
            {buttonSend, richRequest, richResponse, labelRequest, labelResponse});
            panelRightRaw.Dock = DockStyle.Fill;
            panelRightRaw.Name = "panelRightRaw";
            panelRightRaw.Location = new Point(0xd0, 0);
            panelRightRaw.Size = new Size(0x228, 0x1fd);
            panelRightRaw.SizeChanged += PanelRightRaw_SizeChanged;
            splitterRaw.Location = new Point(0xd0, 0);
            splitterRaw.Name = "splitterRaw";
            splitterRaw.Size = new Size(3, 0x1fd);
            splitterRaw.TabStop = false;
            buttonSend.Location = new Point(0x2b8, 0x138);
            buttonSend.FlatStyle = FlatStyle.Popup;
            buttonSend.Name = "buttonSend";
            buttonSend.Size = new Size(0x38, 0x18);
            buttonSend.Text = "Send";
            buttonSend.Click += buttonSend_Click;
            richRequest.Font = Configuration.MasterConfig.UiSettings.ReqRespFont;
            richRequest.Location = new Point(240, 0x18);
            richRequest.Name = "richRequest";
            richRequest.Size = new Size(0x200, 0x110);
            richRequest.Text = "";
            richRequest.WordWrap = false;
            propRequest.Dock = DockStyle.Fill;
            propRequest.CommandsVisibleIfAvailable = true;
            propRequest.HelpVisible = false;
            propRequest.LargeButtons = false;
            propRequest.LineColor = SystemColors.ScrollBar;
            propRequest.Name = "propRequest";
            propRequest.PropertySort = PropertySort.Alphabetical;
            propRequest.Text = "propRequest";
            propRequest.ToolbarVisible = false;
            propRequest.ViewBackColor = SystemColors.Window;
            propRequest.ViewForeColor = SystemColors.WindowText;
            richResponse.Font = Configuration.MasterConfig.UiSettings.ReqRespFont;
            richResponse.Location = new Point(240, 0x158);
            richResponse.Name = "richResponse";
            richResponse.ReadOnly = true;
            richResponse.Size = new Size(0x200, 0x110);
            richResponse.Text = "";
            richResponse.WordWrap = false;
            labelRequest.Location = new Point(240, 8);
            labelRequest.Name = "labelRequest";
            labelRequest.Size = new Size(0x90, 0x10);
            labelRequest.Text = "Request";
            labelResponse.Location = new Point(240, 0x148);
            labelResponse.Name = "labelResponse";
            labelResponse.Size = new Size(0x70, 0x10);
            labelResponse.Text = "Response";
            panelTopMain.BorderStyle = BorderStyle.None;
            panelTopMain.Controls.AddRange(new Control[]
            {labelEndPointUrl, textEndPointUri, buttonBrowseFile, buttonGet});
            panelTopMain.Dock = DockStyle.Top;
            panelTopMain.Name = "panelTopMain";
            panelTopMain.Size = new Size(0, 50);
            panelTopMain.TabIndex = 0;
            panelBottomMain.BorderStyle = BorderStyle.None;
            panelBottomMain.Controls.AddRange(new Control[] { tabMain });
            panelBottomMain.Dock = DockStyle.Fill;
            panelBottomMain.Location = new Point(0, 50);
            panelBottomMain.Name = "panelBottomMain";
            panelBottomMain.TabIndex = 1;
            AutoScaleBaseSize = new Size(5, 13);
            base.ClientSize = new Size(0x2fe, 0x2bf);
            base.Controls.AddRange(new Control[] { panelBottomMain, panelTopMain });
            base.Icon = new Icon(typeof(MainForm), "WebServiceStudio.ico");
            base.MainMenuStrip = mainMenu1;
            base.Name = "MainForm";
            Text = ".NET WebService Studio";
            tabPageInvoke.ResumeLayout(false);
            panelLeftInvoke.ResumeLayout(false);
            panelRightInvoke.ResumeLayout(false);
            tabPageWsdl.ResumeLayout(false);
            panelLeftWsdl.ResumeLayout(false);
            panelRightWsdl.ResumeLayout(false);
            tabPageRaw.ResumeLayout(false);
            tabPageMessage.ResumeLayout(false);
            tabMain.ResumeLayout(false);
            panelTopMain.ResumeLayout(false);
            panelBottomMain.ResumeLayout(false);
            base.ResumeLayout(false);
        }

        private void InvokeWebMethod()
        {
            ServiceOperation operation = treeMethods.SelectedNode?.Tag as ServiceOperation;
            if (operation == null)
            {
                MessageBox.Show(this, "Select a SOAP operation to invoke");
                return;
            }
            var properties = propRequest.SelectedObject as RequestProperties ?? new RequestProperties();
            properties.Method = RequestProperties.HttpMethod.POST;
            properties.Url = operation.Endpoint;
            properties.SOAPAction = operation.Action;
            properties.ContentType = operation.Soap12
                ? "application/soap+xml; charset=utf-8" +
                  (string.IsNullOrEmpty(operation.Action) ? "" : "; action=\"" + operation.Action + "\"")
                : "text/xml; charset=utf-8";
            richRequest.Text = operation.CreateEnvelope(requestParameters);
            properties.requestPayLoad = richRequest.Text;
            propRequest.SelectedObject = properties;
            richResponse.Clear();
            SendWebRequest();
            treeOutput.Nodes.Clear();
            TreeNode response = treeOutput.Nodes.Add(operation.Name + " response");
            if (!string.IsNullOrWhiteSpace(richResponse.Text))
            {
                try
                {
                    new XmlTreeWriter().FillTree(richResponse.Text, response);
                }
                catch (Exception)
                {
                    response.Nodes.Add(richResponse.Text);
                }
                treeOutput.ExpandAll();
            }
        }

        private bool IsValidCopyNode(TreeNodeProperty tnp)
        {
            return (((tnp != null) && (tnp.TreeNode.Parent != null)) && (tnp.GetType() != typeof(TreeNodeProperty)));
        }

        private bool IsValidPasteNode(TreeNodeProperty tnp)
        {
            IDataObject dataObject = Clipboard.GetDataObject();
            if ((dataObject == null) || (dataObject.GetData(DataFormats.Text) == null))
            {
                return false;
            }
            return IsValidCopyNode(tnp);
        }

        [STAThread]
        private static void Main()
        {
            Version version = typeof(string).Assembly.GetName().Version;
            isV1 = ((version.Major == 1) && (version.Minor == 0)) && (version.Build == 0xce4);
            mainForm = new MainForm();
            Application.Run(mainForm);
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            tabMain.Width = (base.Location.X + base.Width) - tabMain.Location.X;
            tabMain.Height = (base.Location.Y + base.Height) - tabMain.Location.Y;
        }

        private void menuItemAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this,
                ".NET Web Service Studio 2.2 \nIdeas and suggestions - Please mailto:adnan@nova.edu");
        }

        private void menuItemExit_Click(object sender, EventArgs e)
        {
            base.Close();
        }

        private void menuItemFind_Click(object sender, EventArgs e)
        {
            var dialog = new SearchDialog();
            dialog.ShowDialog();
            if (dialog.DialogResult == DialogResult.OK)
            {
                tabMain.SelectedTab = tabPageWsdl;
                findOption = RichTextBoxFinds.None;
                if (dialog.MatchCase)
                {
                    findOption |= RichTextBoxFinds.MatchCase;
                }
                if (dialog.WholeWord)
                {
                    findOption |= RichTextBoxFinds.WholeWord;
                }
                searchStr = dialog.SearchStr;
                Find();
            }
        }

        private void menuItemFindNext_Click(object sender, EventArgs e)
        {
            if (tabMain.SelectedTab == tabPageInvoke)
            {
                MessageBox.Show(this, "'Find' cannot be used in the 'Invoke' tab");
            }
            else
            {
                Find();
            }
        }

        private void menuItemHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, MiniHelpText);
        }

        private void menuItemOpen_Click(object sender, EventArgs e)
        {
            openWsdlDialog.ShowDialog();
            string fileName = openWsdlDialog.FileName;
            Cursor cursor = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                wsdl.Reset();
                wsdl.Paths.Add(fileName);
                wsdl.Generate();
                FillWsdlTab();
                FillInvokeTab();
            }
            finally
            {
                Cursor = cursor;
            }
        }

        private void menuItemOptions_Click(object sender, EventArgs e)
        {
            new OptionDialog().ShowDialog();
        }

        private void menuItemSaveAll_Click(object sender, EventArgs e)
        {
            if ((saveAllDialog.ShowDialog() == DialogResult.OK) &&
            (((wsdl.Wsdls != null) && (wsdl.Wsdls.Count != 0)) ||
             ((wsdl.Xsds != null) && (wsdl.Xsds.Count != 0))))
            {
                int length = saveAllDialog.FileName.LastIndexOf('.');
                string str = (length >= 0) ? saveAllDialog.FileName.Substring(0, length) : saveAllDialog.FileName;
                if (wsdl.Wsdls.Count == 1)
                {
                    SaveFile(str + ".wsdl", wsdl.Wsdls[0]);
                }
                else
                {
                    for (int i = 0; i < wsdl.Wsdls.Count; i++)
                    {
                        SaveFile(str + i + ".wsdl", wsdl.Wsdls[i]);
                    }
                }
                if (wsdl.Xsds.Count == 1)
                {
                    SaveFile(str + ".xsd", wsdl.Xsds[0]);
                }
                else
                {
                    for (int j = 0; j < wsdl.Xsds.Count; j++)
                    {
                        SaveFile(str + j + ".xsd", wsdl.Xsds[j]);
                    }
                }
            }
        }

        private void PanelLeftRaw_SizeChanged(object sender, EventArgs e)
        {
            propRequest.SetBounds(0, 0, panelLeftRaw.Width, panelLeftRaw.Height, BoundsSpecified.Size);
        }

        private void PanelRightInvoke_SizeChanged(object sender, EventArgs e)
        {
            int width = (panelRightInvoke.Width - 0x18) / 2;
            int x = 8;
            int num3 = (8 + width) + 8;
            int height = (((panelRightInvoke.Height - 0x10) - 20) - 40) / 2;
            int y = 8;
            int num6 = (0x1c + height) + 20;
            labelInput.SetBounds(x, y, 0, 0, BoundsSpecified.Location);
            labelInputValue.SetBounds(num3, y, 0, 0, BoundsSpecified.Location);
            labelOutput.SetBounds(x, num6, 0, 0, BoundsSpecified.Location);
            labelOutputValue.SetBounds(num3, num6, 0, 0, BoundsSpecified.Location);
            y += 20;
            num6 += 20;
            treeInput.SetBounds(x, y, width, height, BoundsSpecified.All);
            treeOutput.SetBounds(x, num6, width, height, BoundsSpecified.All);
            propInput.SetBounds(num3, y, width, height, BoundsSpecified.All);
            propOutput.SetBounds(num3, num6, width, height, BoundsSpecified.All);
            buttonInvoke.SetBounds((num3 + width) - buttonInvoke.Width,
                ((panelRightInvoke.Height + 20) - buttonInvoke.Height) / 2, 0, 0, BoundsSpecified.Location);
        }

        private void PanelRightRaw_SizeChanged(object sender, EventArgs e)
        {
            int width = panelRightRaw.Width - 0x10;
            int x = 8;
            int height = (((panelRightRaw.Height - 0x10) - 20) - 40) / 2;
            int y = 8;
            int num5 = (0x1c + height) + 20;
            labelRequest.SetBounds(x, y, 0, 0, BoundsSpecified.Location);
            labelResponse.SetBounds(x, num5, 0, 0, BoundsSpecified.Location);
            y += 20;
            num5 += 20;
            richRequest.SetBounds(x, y, width, height, BoundsSpecified.All);
            richResponse.SetBounds(x, num5, width, height, BoundsSpecified.All);
            buttonSend.SetBounds((x + width) - buttonSend.Width, ((panelRightRaw.Height + 20) - buttonSend.Height) / 2, 0,
                0, BoundsSpecified.Location);
        }

        private bool SaveFile(string fileName, string contents)
        {
            if (File.Exists(fileName) &&
                (MessageBox.Show(this, "File " + fileName + " already exists. Overwrite?", "Warning",
                    MessageBoxButtons.YesNo) != DialogResult.Yes))
            {
                return false;
            }
            FileStream stream = File.OpenWrite(fileName);
            var writer = new StreamWriter(stream);
            writer.Write(contents);
            writer.Flush();
            stream.SetLength(stream.Position);
            stream.Close();
            return true;
        }

        private void SendWebRequest()
        {
            var selectedObject = propRequest.SelectedObject as RequestProperties ?? new RequestProperties();
            selectedObject.requestPayLoad = richRequest.Text;
            try
            {
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = selectedObject.AllowAutoRedirect,
                    UseCookies = selectedObject.UseCookieContainer,
                    CookieContainer = selectedObject.CookieContainer,
                    UseProxy = !string.IsNullOrEmpty(selectedObject.HttpProxy),
                    Proxy = string.IsNullOrEmpty(selectedObject.HttpProxy)
                        ? null
                        : new WebProxy(selectedObject.HttpProxy),
                    PreAuthenticate = selectedObject.PreAuthenticate,
                    UseDefaultCredentials = selectedObject.UseDefaultCredential
                };
                if (!string.IsNullOrEmpty(selectedObject.BasicAuthUserName))
                {
                    handler.UseDefaultCredentials = false;
                    handler.Credentials = new NetworkCredential(selectedObject.BasicAuthUserName,
                        selectedObject.BasicAuthPassword);
                }
                using (handler)
                using (var client = new HttpClient(handler))
                using (var request = new HttpRequestMessage(new HttpMethod(selectedObject.Method.ToString()),
                           selectedObject.Url))
                {
                    client.Timeout = selectedObject.Timeout <= 0
                        ? Timeout.InfiniteTimeSpan
                        : TimeSpan.FromMilliseconds(selectedObject.Timeout);
                    if (selectedObject.Method == RequestProperties.HttpMethod.POST)
                    {
                        request.Content = new StringContent(richRequest.Text, Encoding.UTF8);
                        if (!string.IsNullOrEmpty(selectedObject.ContentType))
                        {
                            request.Content.Headers.ContentType =
                                System.Net.Http.Headers.MediaTypeHeaderValue.Parse(selectedObject.ContentType);
                        }
                    }
                    if (!string.IsNullOrEmpty(selectedObject.SOAPAction) &&
                        !(selectedObject.ContentType ?? "").StartsWith("application/soap+xml",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        request.Headers.TryAddWithoutValidation("SOAPAction", "\"" + selectedObject.SOAPAction + "\"");
                    }
                    using (HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult())
                    {
                        richResponse.Text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        selectedObject.responsePayLoad = richResponse.Text;
                    }
                }
            }
            catch (Exception exception)
            {
                richResponse.Text = exception.ToString();
            }
        }

        public static void ShowMessage(object sender, MessageType status, string message)
        {
            if (mainForm != null)
            {
                mainForm.ShowMessageInternal(sender, status, message);
            }
        }

        private void ShowMessageInternal(object sender, MessageType status, string message)
        {
            if (message == null)
            {
                message = status.ToString();
            }
            switch (status)
            {
                case MessageType.Begin:
                    richMessage.SelectionColor = Color.Blue;
                    richMessage.AppendText(message + "\n");
                    richMessage.Update();
                    break;

                case MessageType.Success:
                    richMessage.SelectionColor = Color.Green;
                    richMessage.AppendText(message + "\n");
                    richMessage.Update();
                    if (sender == wsdl)
                    {
                        base.BeginInvoke(new WsdlGenerationDoneCallback(WsdlGenerationDone), new object[] { true });
                    }
                    break;

                case MessageType.Failure:
                    richMessage.SelectionColor = Color.Red;
                    richMessage.AppendText(message + "\n");
                    richMessage.Update();
                    if (sender == wsdl)
                    {
                        base.BeginInvoke(new WsdlGenerationDoneCallback(WsdlGenerationDone), new object[] { false });
                    }
                    break;

                case MessageType.Warning:
                    richMessage.SelectionColor = Color.DarkRed;
                    richMessage.AppendText(message + "\n");
                    richMessage.Update();
                    break;

                case MessageType.Error:
                    richMessage.SelectionColor = Color.Red;
                    richMessage.AppendText(message + "\n");
                    richMessage.Update();
                    break;
            }
        }

        private void tabMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabMain.SelectedTab == tabPageRaw)
            {
                if (propRequest.SelectedObject == null)
                {
                    propRequest.SelectedObject = new RequestProperties();
                }
            }
        }

        private void textEndPointUri_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar == '\r') || (e.KeyChar == '\n'))
            {
                buttonGet_Click(sender, null);
                e.Handled = true;
            }
            else if (!char.IsControl(e.KeyChar))
            {
                if (!isV1)
                {
                    textEndPointUri.SelectedText = e.KeyChar.ToString();
                }
                e.Handled = true;
                string text = textEndPointUri.Text;
                if ((text != null) && (text.Length != 0))
                {
                    for (int i = 0; i < textEndPointUri.Items.Count; i++)
                    {
                        if (((string)textEndPointUri.Items[i]).StartsWith(text))
                        {
                            textEndPointUri.SelectedIndex = i;
                            textEndPointUri.Select(text.Length, textEndPointUri.Text.Length);
                            break;
                        }
                    }
                }
            }
        }

        private void treeInput_AfterSelect(object sender, TreeViewEventArgs e)
        {
            propInput.SelectedObject = e.Node.Tag;
            var property = e.Node.Tag as TreeNodeProperty;
            menuItemTreeInputCopy.Enabled = IsValidCopyNode(property);
            menuItemTreeInputPaste.Enabled = IsValidPasteNode(property);
        }

        private void treeInputMenuCopy_Click(object sender, EventArgs e)
        {
            var property = treeInput.SelectedNode?.Tag as TreeNodeProperty;
            if (IsValidCopyNode(property))
            {
                CopyToClipboard(property);
            }
        }

        private void treeInputMenuPaste_Click(object sender, EventArgs e)
        {
            var tag = treeInput.SelectedNode.Tag as TreeNodeProperty;
            if (tag == null)
            {
                return;
            }
            Type[] typeList = tag.GetTypeList();
            Type type = typeof(DataSet).IsAssignableFrom(typeList[0]) ? typeof(DataSet) : typeof(object);
            var serializer = new XmlSerializer(type, typeList);
            using (var textReader = new StringReader((string)Clipboard.GetDataObject().GetData(DataFormats.Text)))
            {
                object val = serializer.Deserialize(textReader);
                if ((val == null) || !typeList[0].IsInstanceOfType(val))
                {
                    throw new Exception("Invalid Type pasted");
                }
                TreeNodeProperty property2 = TreeNodeProperty.CreateTreeNodeProperty(tag, val);
                property2.TreeNode = tag.TreeNode;
                property2.RecreateSubtree(null);
                treeInput.SelectedNode = property2.TreeNode;
                textReader.Close();
            }
        }

        private void treeMethods_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ServiceOperation operation = e.Node.Tag as ServiceOperation;
            if (operation != null)
            {
                requestParameters = operation.CreateParameters();
                treeInput.Nodes.Clear();
                foreach (SoapParameter parameter in requestParameters)
                {
                    AddParameterNode(treeInput.Nodes, parameter);
                }
                richRequest.Text = operation.CreateEnvelope(requestParameters);
                propRequest.SelectedObject = new RequestProperties
                {
                    Method = RequestProperties.HttpMethod.POST,
                    Url = operation.Endpoint,
                    SOAPAction = operation.Action,
                    ContentType = operation.Soap12
                        ? "application/soap+xml; charset=utf-8" +
                          (string.IsNullOrEmpty(operation.Action) ? "" : "; action=\"" + operation.Action + "\"")
                        : "text/xml; charset=utf-8",
                    requestPayLoad = richRequest.Text,
                    AllowAutoRedirect = true,
                    Timeout = 10000
                };
                propInput.SelectedObject = null;
                treeOutput.Nodes.Clear();
                propOutput.SelectedObject = null;
                if (treeInput.Nodes.Count > 0)
                {
                    treeInput.SelectedNode = treeInput.Nodes[0];
                }
            }
            treeInput.ExpandAll();
        }

        private static void AddParameterNode(TreeNodeCollection nodes, SoapParameter parameter)
        {
            TreeNode node = nodes.Add(parameter.Name);
            node.Tag = parameter;
            foreach (SoapParameter child in parameter.Children)
            {
                AddParameterNode(node.Nodes, child);
            }
        }

        private void treeOutput_AfterSelect(object sender, TreeViewEventArgs e)
        {
            propOutput.SelectedObject = e.Node.Tag;
            menuItemTreeOutputCopy.Enabled = IsValidCopyNode(e.Node.Tag as TreeNodeProperty);
        }

        private void treeOutputMenuCopy_Click(object sender, EventArgs e)
        {
            CopyToClipboard(treeOutput.SelectedNode.Tag as TreeNodeProperty);
        }

        private void treeWsdl_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if ((e.Node.Tag != null) && !Equals(richWsdl.Tag, e.Node.Tag))
            {
                richWsdl.Text = e.Node.Tag.ToString();
                richWsdl.Tag = e.Node.Tag;
            }
            var node = e.Node as XmlTreeNode;
            if (node != null)
            {
                richWsdl.Select(node.StartPosition, node.EndPosition - node.StartPosition);
            }
        }

        private void WsdlGenerationDone(bool genDone)
        {
            buttonGet.Text = "Get";
            FillWsdlTab();
            if (genDone)
            {
                ShowMessageInternal(this, MessageType.Begin, "Loading SOAP operations");
                FillInvokeTab();
                tabMain.SelectedTab = tabPageInvoke;
                ShowMessageInternal(this, MessageType.Success, "Ready To Invoke");
                Configuration.MasterConfig.InvokeSettings.AddUri(textEndPointUri.Text);
                textEndPointUri.Items.Clear();
                textEndPointUri.Items.AddRange(Configuration.MasterConfig.InvokeSettings.RecentlyUsedUris);
            }
        }

        private delegate void WsdlGenerationDoneCallback(bool genDone);
    }
}