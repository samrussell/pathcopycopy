﻿// AdvancedPipelinePluginForm.cs
// (c) 2019-2021, Charles Lechasseur
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using PathCopyCopy.Settings.Core.Plugins;
using PathCopyCopy.Settings.Properties;
using PathCopyCopy.Settings.UI.UserControls;
using PathCopyCopy.Settings.UI.Utils;

namespace PathCopyCopy.Settings.UI.Forms
{
    /// <summary>
    /// Form used to create or edit a pipeline plugin in Expert Mode. To use,
    /// create the form and call the <see cref="EditPlugin"/> method.
    /// </summary>
    public partial class AdvancedPipelinePluginForm : PositionPersistedForm
    {
        /// Initial plugin info used to populate our controls.
        /// Will be null if we're creating a new pipeline plugin.
        private PipelinePluginInfo oldPluginInfo;

        /// Pipeline of the initial plugin, if we have one.
        private Pipeline oldPipeline;

        /// ID of the plugin we're editing. Will be generated
        /// if we're creating a new pipeline plugin.
        private Guid pluginId;

        /// Binding list that will store the pipeline elements so that we can use data binding.
        private readonly BindingList<PipelineElement> elements = new BindingList<PipelineElement>();

        /// User control to edit currently-selected pipeline element.
        private PipelineElementUserControl currentUserControl;

        /// Plugin info of the plugin being edited. Updated as
        /// the control changes. Will be returned if user chooses OK.
        private PipelinePluginInfo newPluginInfo;

        /// Pipeline of the new plugin info, if we have one.
        private Pipeline newPipeline;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AdvancedPipelinePluginForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Main method to use the form. Will show the form as a modal dialog,
        /// allowing the user to edit the given pipeline plugin. When the user
        /// is done, the method will return the new <see cref="PipelinePluginInfo"/>
        /// if the user accepted the changes.
        /// </summary>
        /// <param name="owner">Owner of this dialog. Can be <c>null</c>.</param>
        /// <param name="oldInfo">Info about a pipeline plugin. If set, we'll
        /// populate the form with the plugin's values to allow the user to
        /// edit the plugin.</param>
        /// <param name="switchToSimple">Upon exit, will indicate whether user
        /// chose to switch to Simple Mode.</param>
        /// <returns>Info about the new plugin that user edited. Will be
        /// <c>null</c> if user cancels editing.</returns>
        public PipelinePluginInfo EditPlugin(IWin32Window owner, PipelinePluginInfo oldInfo,
            out bool switchToSimple)
        {
            // Save old info so that the OnLoad event handler can use it.
            oldPluginInfo = oldInfo;

            // If a plugin info was specified, decode its pipeline immediately.
            // We want pipeline exceptions to propagate out *before* we show the form.
            if (oldPluginInfo != null) {
                oldPipeline = PipelineDecoder.DecodePipeline(oldPluginInfo.EncodedElements);
            }

            // Save plugin ID, or generate a new one if this is a new plugin.
            if (oldPluginInfo != null) {
                pluginId = oldPluginInfo.Id;
            } else {
                pluginId = Guid.NewGuid();
            }

            // Show the form and check result.
            DialogResult dialogRes = ShowDialog(owner);

            // If user saved, return the new info.
            Debug.Assert(dialogRes == DialogResult.Cancel || newPluginInfo != null);
            switchToSimple = dialogRes == DialogResult.Retry;
            return dialogRes != DialogResult.Cancel ? newPluginInfo : null;
        }

        

        /// <summary>
        /// Called when the form is loaded. We use this opportunity to load the
        /// controls necessary to edit the pipeline plugin.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void AdvancedPipelinePluginForm_Load(object sender, EventArgs e)
        {
            // The initial text of the MinVersionLbl control is a format string.
            // Save it in its Tag to be able to reuse it.
            MinVersionLbl.Tag = MinVersionLbl.Text;

            // Populate the context menu strip used to create new elements.
            // We do this in code to be able to reuse resources to avoid string duplication.
            AddNewElementMenuItem(Resources.PipelineElement_ApplyPipelinePlugin,
                Resources.PipelineElement_ApplyPipelinePlugin_HelpText,
                () => new ApplyPipelinePluginPipelineElement(new Guid(Resources.LONG_PATH_PLUGIN_ID)));
            AddNewElementMenuItem(Resources.PipelineElement_ApplyPlugin,
                Resources.PipelineElement_ApplyPlugin_HelpText,
                () => new ApplyPluginPipelineElement(new Guid(Resources.LONG_PATH_PLUGIN_ID)));
            AddNewElementMenuItem("-", null, null);
            AddNewElementMenuItem(Resources.PipelineElement_RemoveExt,
                Resources.PipelineElement_RemoveExt_HelpText,
                () => new RemoveExtPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_Quotes,
                Resources.PipelineElement_Quotes_HelpText,
                () => new QuotesPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_OptionalQuotes,
                Resources.PipelineElement_OptionalQuotes_HelpText,
                () => new OptionalQuotesPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_EmailLinks,
                Resources.PipelineElement_EmailLinks_HelpText,
                () => new EmailLinksPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_EncodeURIWhitespace,
                Resources.PipelineElement_EncodeURIWhitespace_HelpText,
                () => new EncodeURIWhitespacePipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_EncodeURIChars,
                Resources.PipelineElement_EncodeURIChars_HelpText,
                () => new EncodeURICharsPipelineElement());
            AddNewElementMenuItem("-", null, null);
            AddNewElementMenuItem(Resources.PipelineElement_ForwardToBackslashes,
                Resources.PipelineElement_ForwardToBackslashes_HelpText,
                () => new ForwardToBackslashesPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_BackToForwardSlashes,
                Resources.PipelineElement_BackToForwardSlashes_HelpText,
                () => new BackToForwardSlashesPipelineElement());
            AddNewElementMenuItem("-", null, null);
            AddNewElementMenuItem(Resources.PipelineElement_FindReplace,
                Resources.PipelineElement_FindReplace_HelpText,
                () => new FindReplacePipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_Regex,
                Resources.PipelineElement_Regex_HelpText,
                () => new RegexPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_UnexpandEnvStrings,
                Resources.PipelineElement_UnexpandEnvStrings_HelpText,
                () => new UnexpandEnvironmentStringsPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_InjectDriveLabel,
                Resources.PipelineElement_InjectDriveLabel_HelpText,
                () => new InjectDriveLabelPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_CopyNPathParts,
                Resources.PipelineElement_CopyNPathParts_HelpText,
                () => new CopyNPathPartsPipelineElement());
            AddNewElementMenuItem("-", null, null);
            AddNewElementMenuItem(Resources.PipelineElement_FollowSymlink,
                Resources.PipelineElement_FollowSymlink_HelpText,
                () => new FollowSymlinkPipelineElement());
            AddNewElementMenuItem("-", null, null);
            AddNewElementMenuItem(Resources.PipelineElement_PushToStack,
                Resources.PipelineElement_PushToStack_HelpText,
                () => new PushToStackPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_PopFromStack,
                Resources.PipelineElement_PopFromStack_HelpText,
                () => new PopFromStackPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_SwapStackValues,
                Resources.PipelineElement_SwapStackValues_HelpText,
                () => new SwapStackValuesPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_DuplicateStackValue,
                Resources.PipelineElement_DuplicateStackValue_HelpText,
                () => new DuplicateStackValuePipelineElement());
            AddNewElementMenuItem("-", null, null);
            AddNewElementMenuItem(Resources.PipelineElement_PathsSeparator,
                Resources.PipelineElement_PathsSeparator_HelpText,
                () => new PathsSeparatorPipelineElement(PipelinePluginEditor.PATHS_SEPARATOR_ON_SAME_LINE));
            AddNewElementMenuItem(Resources.PipelineElement_RecursiveCopy,
                Resources.PipelineElement_RecursiveCopy_HelpText,
                () => new RecursiveCopyPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_Executable,
                Resources.PipelineElement_Executable_HelpText,
                () => new ExecutablePipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_ExecutableWithFilelist,
                Resources.PipelineElement_ExecutableWithFilelist_HelpText,
                () => new ExecutableWithFilelistPipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_CommandLine,
                Resources.PipelineElement_CommandLine_HelpText,
                () => new CommandLinePipelineElement());
            AddNewElementMenuItem(Resources.PipelineElement_DisplayForSelection,
                Resources.PipelineElement_DisplayForSelection_HelpText,
                () => new DisplayForSelectionPipelineElement());

            if (oldPipeline != null) {
                // Copy pipeline elements from the pipeline to a list that supports data binding.
                // This is needed otherwise the list box won't properly function.
                foreach (PipelineElement element in oldPipeline.Elements) {
                    elements.Add(element);
                }
            }

            // Populate our controls.
            NameTxt.Text = oldPluginInfo?.Description ?? string.Empty;
            ElementsLst.DataSource = elements;

            // Update initial controls.
            UpdateControls();

            // Immediately update plugin info so that preview box is initially filled.
            UpdatePluginInfo();
        }

        /// <summary>
        /// Updates <see cref="newPluginInfo"/> with the contents of the
        /// form's controls. Also updates the preview box.
        /// </summary>
        private void UpdatePluginInfo()
        {
            // Create new pipeline and copy elements back from the binding list.
            Pipeline pipeline = new Pipeline();
            pipeline.Elements.AddRange(elements);

            // Create new plugin info and save encoded elements.
            PipelinePluginInfo pluginInfo = new PipelinePluginInfo {
                Id = pluginId,
                Description = NameTxt.Text,
                EncodedElements = pipeline.Encode(),
                RequiredVersion = pipeline.RequiredVersion,
                EditMode = PipelinePluginEditMode.Expert,
            };
            Debug.Assert(!pluginInfo.Global);

            // Save plugin info and pipeline, then update preview and min version.
            newPluginInfo = pluginInfo;
            newPipeline = pipeline;
            PreviewCtrl.Plugin = newPluginInfo.ToPlugin();
            Version requiredVersion = newPipeline.RequiredVersion;
            int numComponents;
            if (requiredVersion.Revision > 0) {
                numComponents = 4;
            } else if (requiredVersion.Build > 0) {
                numComponents = 3;
            } else {
                numComponents = 2;
            }
            MinVersionLbl.Text = string.Format(CultureInfo.CurrentCulture,
                MinVersionLbl.Tag as string, requiredVersion.ToString(numComponents));
        }

        /// <summary>
        /// Called when the form is about to close. If user pressed OK, we save
        /// the content of plugin here.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void AdvancedPipelinePluginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If user chose to press OK or switch to Simple Mode, save plugin info.
            if (DialogResult == DialogResult.OK || DialogResult == DialogResult.Retry) {
                // Make sure user has entered a name (unless we're switching to Simple Mode).
                if (!string.IsNullOrEmpty(NameTxt.Text) || DialogResult == DialogResult.Retry) {
                    // Update plugin info so that we have a pipeline.
                    UpdatePluginInfo();

                    // If pipeline is too complex, user might lose customization by switching
                    // to simple mode. Warn in this case.
                    Debug.Assert(newPipeline != null);
                    if (DialogResult == DialogResult.Retry && !PipelinePluginEditor.IsPipelineSimple(newPipeline)) {
                        DialogResult subDialogRes = MessageBox.Show(Resources.PipelinePluginForm_PipelineTooComplexForSimpleMode,
                            Resources.PipelinePluginForm_MsgTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (subDialogRes == DialogResult.No) {
                            e.Cancel = true;
                        }
                    }
                } else {
                    // Warn user that we need a non-empty name.
                    MessageBox.Show(Resources.PipelinePluginForm_EmptyName, Resources.PipelinePluginForm_MsgTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    NameTxt.Focus();
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        /// Adds a new item to the contextual menu strip used to create
        /// new pipeline elements.
        /// </summary>
        /// <param name="description">Menu item description.</param>
        /// <param name="helpText">Menu item help text. Will be shown in a tooltip
        /// when the mouse hovers over the item.</param>
        /// <param name="creator">Function used to instanciate a new pipeline element.</param>
        private void AddNewElementMenuItem(string description, string helpText,
            Func<PipelineElement> creator)
        {
            ToolStripItem newItem = NewElementContextMenuStrip.Items.Add(description);
            if (!string.IsNullOrEmpty(helpText)) {
                newItem.AutoToolTip = false;
                newItem.ToolTipText = helpText;
            }
            if (creator != null) {
                newItem.Tag = creator;
            }
            newItem.Click += NewElementMenuItem_Click;
        }

        /// <summary>
        /// Updates controls that are dependent on the list selection.
        /// </summary>
        private void UpdateControls()
        {
            DeleteElementBtn.Enabled = ElementsLst.SelectedIndex >= 0;
            MoveElementUpBtn.Enabled = ElementsLst.SelectedIndex > 0;
            MoveElementDownBtn.Enabled = ElementsLst.SelectedIndex >= 0 &&
                ElementsLst.SelectedIndex < (elements.Count - 1);
        }

        /// <summary>
        /// Called when a new pipeline element is selected in the list.
        /// We must show the appropriate user control to enable editing.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ElementsLst_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Suspend layout operations while we modify our controls.
            SuspendLayout();
            try {
                if (currentUserControl != null) {
                    // We currently display a user control for another element, remove it.
                    currentUserControl.PipelineElementChanged -= PipelineElementUserControl_PipelineElementChanged;
                    Controls.Remove(currentUserControl);
                    currentUserControl.Dispose();
                    currentUserControl = null;
                }

                // Show or hide label instructing user to select an element.
                SelectElementLbl.Visible = ElementsLst.SelectedIndex < 0;

                // If user selected an element, display its control.
                if (ElementsLst.SelectedIndex >= 0) {
                    currentUserControl = elements[ElementsLst.SelectedIndex].GetEditingControl();
                    currentUserControl.Visible = false;
                    Controls.Add(currentUserControl);
                    currentUserControl.Location = UserControlPlacementPanel.Location;
                    currentUserControl.Size = UserControlPlacementPanel.Size;
                    currentUserControl.Anchor = UserControlPlacementPanel.Anchor;
                    currentUserControl.TabIndex = UserControlPlacementPanel.TabIndex;
                    currentUserControl.PipelineElementChanged += PipelineElementUserControl_PipelineElementChanged;
                    currentUserControl.Visible = true;
                }

                // Update selection-dependent controls.
                UpdateControls();
            } finally {
                ResumeLayout(true);
            }
        }

        /// <summary>
        /// Called when the user clicks the button to create a new pipeline element.
        /// We must display a drop-down menu with the choice of element types.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void NewElementBtn_Click(object sender, EventArgs e)
        {
            NewElementContextMenuStrip.Show(NewElementBtn, new Point(0, NewElementBtn.Size.Height));
        }

        /// <summary>
        /// Called when the user selects a menu item to create a new pipeline element.
        /// We need to instanciate the new element and add it to the pipeline.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void NewElementMenuItem_Click(object sender, EventArgs e)
        {
            // A function to create the new element is stored in the item's Tag.
            if (((ToolStripMenuItem) sender).Tag is Func<PipelineElement> creator) {
                // Instanciate element and add it to the end of the pipeline.
                elements.Add(creator());

                // Select the new pipeline element so that user can edit it.
                ElementsLst.SelectedIndex = elements.Count - 1;

                // Update our selection-dependent controls.
                UpdateControls();

                // Update preview.
                UpdatePluginInfo();
            }
        }

        /// <summary>
        /// Called when the user clicks on the button to delete a pipeline element.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void DeleteElementBtn_Click(object sender, EventArgs e)
        {
            // Remove selected element.
            int selectedIdx = ElementsLst.SelectedIndex;
            elements.RemoveAt(selectedIdx);

            // This won't trigger selected index changed event, so deselect and reselect the item.
            ElementsLst.SelectedIndex = -1;
            if (elements.Count > selectedIdx) {
                ElementsLst.SelectedIndex = selectedIdx;
            } else if (elements.Count > 0) {
                ElementsLst.SelectedIndex = elements.Count - 1;
            }

            // Update selection-dependent controls.
            UpdateControls();

            // Update preview.
            UpdatePluginInfo();
        }

        /// <summary>
        /// Called when the user clicks on the button to move a pipeline element
        /// up towards the beginning of the pipeline.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void MoveElementUpBtn_Click(object sender, EventArgs e)
        {
            // We need to remove element and reinsert it.
            int selectedIdx = ElementsLst.SelectedIndex;
            PipelineElement element = elements[selectedIdx];
            elements.RemoveAt(selectedIdx);
            elements.Insert(selectedIdx - 1, element);

            // Reselect the element and update our selection-dependent controls.
            ElementsLst.SelectedIndex = selectedIdx - 1;
            UpdateControls();

            // Update preview.
            UpdatePluginInfo();
        }

        /// <summary>
        /// Called when the user clicks on the button to move a pipeline element
        /// down towards the end of the pipeline.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void MoveElementDownBtn_Click(object sender, EventArgs e)
        {
            // We need to remove element and reinsert it.
            int selectedIdx = ElementsLst.SelectedIndex;
            PipelineElement element = elements[selectedIdx];
            elements.RemoveAt(selectedIdx);
            elements.Insert(selectedIdx + 1, element);

            // Reselect the element and update our selection-dependent controls.
            ElementsLst.SelectedIndex = selectedIdx + 1;
            UpdateControls();

            // Update preview.
            UpdatePluginInfo();
        }

        /// <summary>
        /// Called when the currently-edited pipeline element changes. We use this
        /// opportunity to update the pipeline plugin info and the preview.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void PipelineElementUserControl_PipelineElementChanged(object sender, EventArgs e)
        {
            UpdatePluginInfo();
        }

        /// <summary>
        /// Called when the user presses the Help button in the form's caption bar.
        /// We navigate to the wiki to show help in such a case.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void AdvancedPipelinePluginForm_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            // Open wiki link to Expert Mode page, then cancel the event to avoid
            // displaying a help mouse pointer like the default behavior.
            Process.Start(Resources.WikiLink_CustomCommandsExpertMode);
            e.Cancel = true;
        }
    }
}
