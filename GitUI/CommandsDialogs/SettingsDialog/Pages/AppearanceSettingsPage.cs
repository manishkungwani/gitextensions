﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GitCommands;
using GitCommands.Utils;
using GitExtUtils.GitUI;
using GitUI.Avatars;
using GitUIPluginInterfaces;
using ResourceManager;

namespace GitUI.CommandsDialogs.SettingsDialog.Pages
{
    public partial class AppearanceSettingsPage : SettingsPageWithHeader
    {
        private const string _customAvatarTemplateURL = "https://git-extensions-documentation.readthedocs.io/en/latest/settings.html#git-extensions-appearance-author-images-avatar-provider";
        private const string _gravatarDefaultImageURL = "https://git-extensions-documentation.readthedocs.io/en/latest/settings.html#git-extensions-appearance-author-images-avatar-fallback";
        private const string _spellingWikiURL = "https://github.com/gitextensions/gitextensions/wiki/Spelling";
        private const string _translationsWikiURL = "https://github.com/gitextensions/gitextensions/wiki/Translations";

        private readonly TranslationString _noDictFile = new("None");
        private readonly TranslationString _noDictFilesFound = new("No dictionary files found in: {0}");
        private readonly TranslationString _noImageServiceTooltip = new($"A default image, if the provider has no image for the email address.\r\n\r\nClick this info icon for more details.");
        private readonly TranslationString _authorDateSortWarningTooltip = new("Sorting by author date may delay rendering of the revision graph.");
        private readonly TranslationString _avatarProviderTooltip = new($"The avatar provider defines the source for user-defined avatar images.\r\nThe \"Default\" provider uses GitHub and Gravatar,\r\nthe \"Custom\" provider allows you to set custom provider URLs and\r\n\"None\" disables user-defined avatars.\r\n\r\nClick this info icon for more details.");

        public AppearanceSettingsPage()
        {
            InitializeComponent();
            Text = "Appearance";
            InitializeComplete();

            FillComboBoxWithEnumValues<GitRefsSortOrder>(_NO_TRANSLATE_cmbBranchesOrder);
            FillComboBoxWithEnumValues<GitRefsSortBy>(_NO_TRANSLATE_cmbBranchesSortBy);
            FillComboBoxWithEnumValues<AvatarProvider>(AvatarProvider);
            FillComboBoxWithEnumValues<AvatarFallbackType>(_NO_TRANSLATE_NoImageService);
        }

        private void FillComboBoxWithEnumValues<T>(ComboBox comboBox) where T : Enum
        {
            comboBox.DisplayMember = nameof(ComboBoxItem<T>.Text);
            comboBox.ValueMember = nameof(ComboBoxItem<T>.Value);
            comboBox.DataSource = EnumHelper.GetValues<T>()
                .Select(e => new ComboBoxItem<T> { Text = e.GetDescription(), Value = e })
                .ToArray();
        }

        protected override void OnRuntimeLoad()
        {
            base.OnRuntimeLoad();

            ToolTip.SetToolTip(_NO_TRANSLATE_NoImageService, _noImageServiceTooltip.Text);
            ToolTip.SetToolTip(pictureAvatarHelp, _noImageServiceTooltip.Text);
            ToolTip.SetToolTip(avatarProviderHelp, _avatarProviderTooltip.Text);
            chkSortByAuthorDate.ToolTipText = _authorDateSortWarningTooltip.Text;
            pictureAvatarHelp.Size = DpiUtil.Scale(pictureAvatarHelp.Size);
            avatarProviderHelp.Size = DpiUtil.Scale(avatarProviderHelp.Size);

            // align 1st columns across all tables
            tlpnlGeneral.AdjustWidthToSize(0, lblBranchesSortBy, lblBranchesOrder, truncateLongFilenames, lblCacheDays, lblNoImageService, lblLanguage, lblSpellingDictionary);
            tlpnlAuthor.AdjustWidthToSize(0, lblBranchesSortBy, lblBranchesOrder, truncateLongFilenames, lblCacheDays, lblNoImageService, lblLanguage, lblSpellingDictionary);
            tlpnlLanguage.AdjustWidthToSize(0, lblBranchesSortBy, lblBranchesOrder, truncateLongFilenames, lblCacheDays, lblNoImageService, lblLanguage, lblSpellingDictionary);

            // align 2nd columns across all tables
            truncatePathMethod.AdjustWidthToFitContent();
            Language.AdjustWidthToFitContent();
            tlpnlGeneral.AdjustWidthToSize(1, _NO_TRANSLATE_cmbBranchesSortBy, _NO_TRANSLATE_cmbBranchesOrder, truncatePathMethod, _NO_TRANSLATE_NoImageService, Language);
            tlpnlAuthor.AdjustWidthToSize(1, _NO_TRANSLATE_cmbBranchesSortBy, _NO_TRANSLATE_cmbBranchesOrder, truncatePathMethod, _NO_TRANSLATE_NoImageService, Language);
            tlpnlLanguage.AdjustWidthToSize(1, _NO_TRANSLATE_cmbBranchesSortBy, _NO_TRANSLATE_cmbBranchesOrder, truncatePathMethod, _NO_TRANSLATE_NoImageService, Language);
        }

        public static SettingsPageReference GetPageReference()
        {
            return new SettingsPageReferenceByType(typeof(AppearanceSettingsPage));
        }

        protected override void SettingsToPage()
        {
            chkEnableAutoScale.Checked = AppSettings.EnableAutoScale;

            chkShowRepoCurrentBranch.Checked = AppSettings.ShowRepoCurrentBranch;
            chkShowCurrentBranchInVisualStudio.Checked = AppSettings.ShowCurrentBranchInVisualStudio;
            _NO_TRANSLATE_DaysToCacheImages.Value = AppSettings.AvatarImageCacheDays;
            ShowAuthorAvatarInCommitInfo.Checked = AppSettings.ShowAuthorAvatarInCommitInfo;
            ShowAuthorAvatarInCommitGraph.Checked = AppSettings.ShowAuthorAvatarColumn;
            chkSortByAuthorDate.Checked = AppSettings.SortByAuthorDate;
            AvatarProvider.SelectedValue = AppSettings.AvatarProvider;
            _NO_TRANSLATE_NoImageService.SelectedValue = AppSettings.AvatarFallbackType;
            txtCustomAvatarTemplate.Text = AppSettings.CustomAvatarTemplate;
            ManageAvatarOptionsDisplay();

            Language.Items.Clear();
            Language.Items.Add("English");
            Language.Items.AddRange(Translator.GetAllTranslations());
            Language.Text = AppSettings.Translation;

            truncatePathMethod.SelectedIndex = GetTruncatePathMethodIndex(AppSettings.TruncatePathMethod);
            _NO_TRANSLATE_cmbBranchesOrder.SelectedIndex = (int)AppSettings.RefsSortOrder;
            _NO_TRANSLATE_cmbBranchesSortBy.SelectedIndex = (int)AppSettings.RefsSortBy;

            Dictionary.Items.Clear();
            Dictionary.Items.Add(_noDictFile.Text);
            if (AppSettings.Dictionary.Equals("none", StringComparison.InvariantCultureIgnoreCase))
            {
                Dictionary.SelectedIndex = 0;
            }
            else
            {
                string dictionaryFile = string.Concat(Path.Combine(AppSettings.GetDictionaryDir(), AppSettings.Dictionary), ".dic");
                if (File.Exists(dictionaryFile))
                {
                    Dictionary.Items.Add(AppSettings.Dictionary);
                    Dictionary.Text = AppSettings.Dictionary;
                }
                else
                {
                    Dictionary.SelectedIndex = 0;
                }
            }

            chkShowRelativeDate.Checked = AppSettings.RelativeDate;

            return;

            int GetTruncatePathMethodIndex(TruncatePathMethod method)
            {
                switch (method)
                {
                    case TruncatePathMethod.Compact:
                        return 1;
                    case TruncatePathMethod.TrimStart:
                        return 2;
                    case TruncatePathMethod.FileNameOnly:
                        return 3;
                    default:
                        return 0;
                }
            }
        }

        protected override void PageToSettings()
        {
            var shouldClearCache =
                AppSettings.AvatarProvider != (AvatarProvider)AvatarProvider.SelectedValue
                || AppSettings.AvatarFallbackType != (AvatarFallbackType)_NO_TRANSLATE_NoImageService.SelectedValue
                || AppSettings.CustomAvatarTemplate != txtCustomAvatarTemplate.Text;

            AppSettings.EnableAutoScale = chkEnableAutoScale.Checked;
            AppSettings.TruncatePathMethod = GetTruncatePathMethodString(truncatePathMethod.SelectedIndex);
            AppSettings.ShowRepoCurrentBranch = chkShowRepoCurrentBranch.Checked;
            AppSettings.ShowCurrentBranchInVisualStudio = chkShowCurrentBranchInVisualStudio.Checked;
            AppSettings.ShowAuthorAvatarColumn = ShowAuthorAvatarInCommitGraph.Checked;
            AppSettings.ShowAuthorAvatarInCommitInfo = ShowAuthorAvatarInCommitInfo.Checked;
            AppSettings.AvatarImageCacheDays = (int)_NO_TRANSLATE_DaysToCacheImages.Value;
            AppSettings.CustomAvatarTemplate = txtCustomAvatarTemplate.Text;
            AppSettings.SortByAuthorDate = chkSortByAuthorDate.Checked;
            AppSettings.RefsSortOrder = (GitRefsSortOrder)_NO_TRANSLATE_cmbBranchesOrder.SelectedIndex;
            AppSettings.RefsSortBy = (GitRefsSortBy)_NO_TRANSLATE_cmbBranchesSortBy.SelectedIndex;

            AppSettings.Translation = Language.Text;
            ResourceManager.Strings.Reinitialize();
            Strings.Reinitialize();

            AppSettings.AvatarProvider = (AvatarProvider)AvatarProvider.SelectedValue;

            if (_NO_TRANSLATE_NoImageService.SelectedValue is AvatarFallbackType imageType)
            {
                AppSettings.AvatarFallbackType = imageType;
            }

            if (shouldClearCache)
            {
                new AvatarControl().ClearCache();
            }

            AppSettings.RelativeDate = chkShowRelativeDate.Checked;

            AppSettings.Dictionary = Dictionary.SelectedIndex == 0 ? "none" : Dictionary.Text;

            return;

            TruncatePathMethod GetTruncatePathMethodString(int index) => index switch
            {
                1 => TruncatePathMethod.Compact,
                2 => TruncatePathMethod.TrimStart,
                3 => TruncatePathMethod.FileNameOnly,
                _ => TruncatePathMethod.None,
            };
        }

        private void Dictionary_DropDown(object sender, EventArgs e)
        {
            try
            {
                string currentDictionary = Dictionary.Text;

                Dictionary.Items.Clear();
                Dictionary.Items.Add(_noDictFile.Text);
                foreach (
                    string fileName in
                        Directory.GetFiles(AppSettings.GetDictionaryDir(), "*.dic", SearchOption.TopDirectoryOnly))
                {
                    var file = new FileInfo(fileName);
                    Dictionary.Items.Add(file.Name.Replace(".dic", ""));
                }

                Dictionary.Text = currentDictionary;
            }
            catch
            {
                MessageBox.Show(this, string.Format(_noDictFilesFound.Text, AppSettings.GetDictionaryDir()), Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearImageCache_Click(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(AvatarService.CacheCleaner.ClearCacheAsync);
        }

        private void helpTranslate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OsShellUtil.OpenUrlInDefaultBrowser(_translationsWikiURL);
        }

        private void downloadDictionary_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OsShellUtil.OpenUrlInDefaultBrowser(_spellingWikiURL);
        }

        private void pictureAvatarHelp_Click(object sender, EventArgs e)
        {
            OsShellUtil.OpenUrlInDefaultBrowser(_gravatarDefaultImageURL);
        }

        private void customAvatarHelp_Click(object sender, EventArgs e)
        {
            OsShellUtil.OpenUrlInDefaultBrowser(_customAvatarTemplateURL);
        }

        private void AvatarProvider_SelectedIndexChanged(object sender, EventArgs e)
        {
            ManageAvatarOptionsDisplay();
        }

        private void ManageAvatarOptionsDisplay()
        {
            var showCustomTemplate = (AvatarProvider)AvatarProvider.SelectedValue == GitCommands.AvatarProvider.Custom;

            lblCustomAvatarTemplate.Visible = showCustomTemplate;
            txtCustomAvatarTemplate.Visible = showCustomTemplate;
        }

        private class ComboBoxItem<T>
        {
            public string Text { get; set; }
            public T Value { get; set; }
        }
    }
}
