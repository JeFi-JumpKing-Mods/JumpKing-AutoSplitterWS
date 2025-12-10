using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.JumpKingWS.Split;
using LiveSplit.JumpKingWS.Communication;

namespace LiveSplit.JumpKingWS.UI;
public partial class Settings : UserControl
{
    const string checkBox_AutoStart_TIP = "Start timer when game start.";
    const string checkBox_AutoReset_TIP = "Reset timer when game restart.";
    const string checkBox_Undo_TIP = "Undo screen split if player doesn't land on target screen.\n (as IL rule)";
    const string button_Reconnect_TIP = "Try reconnect on both side.";
    public static bool isAutoStartTimer = false;
    public static bool isAutoResetTimer = false;
    public static bool isUndoSplit = false;
    const string ISAUTOSTARTTIMER_NODENAME = "AutoStartTimer";
    const string ISAUTORESETTIMER_NODENAME = "AutoResetTimer";
    const string ISUNDOSPLIT_NODENAME = "UndoSplit";
    private static readonly int HASH_isAutoStartTimer = nameof(isAutoStartTimer).GetHashCode();
    private static readonly int HASH_isAutoResetTimer = nameof(isAutoResetTimer).GetHashCode();
    private static readonly int HASH_isUndoSplit = nameof(isUndoSplit).GetHashCode();
    
    public static void LoadFromXml(XmlNode node)
    {
        if (node!=null)
        {
            bool.TryParse(node[ISAUTOSTARTTIMER_NODENAME]?.InnerText, out isAutoStartTimer);
            bool.TryParse(node[ISAUTORESETTIMER_NODENAME]?.InnerText, out isAutoResetTimer);
            bool.TryParse(node[ISUNDOSPLIT_NODENAME]?.InnerText, out isUndoSplit);
        }
    }
    public static void SaveToXml(XmlDocument doc, XmlElement ele)
    {
		ele.AppendChild(GetXmlKeyValue(doc, ISAUTOSTARTTIMER_NODENAME, isAutoStartTimer));
		ele.AppendChild(GetXmlKeyValue(doc, ISAUTORESETTIMER_NODENAME, isAutoResetTimer));
		ele.AppendChild(GetXmlKeyValue(doc, ISUNDOSPLIT_NODENAME, isUndoSplit));
    }
    private static XmlElement GetXmlKeyValue<T>(XmlDocument document, string key, T value)
	{
		var element = document.CreateElement(key);
		element.InnerText = value.ToString();
		return element; 
	}
    public static int GetHash()
    {
        int hash = 0x6546B5C;
        if (isAutoStartTimer) hash ^= HASH_isAutoStartTimer;
        if (isAutoResetTimer) hash ^= HASH_isAutoResetTimer;
        if (isUndoSplit) hash ^= HASH_isUndoSplit;
        return hash;
    }

    private bool isRegistedFormClosed = false;
    private readonly List<SplitSettingFrame> SplitSettingFrames = [];

    public Settings()
    {
        InitializeComponent();
    }

    private void Settings_Load(object sender, EventArgs e)
    {
        Form form = FindForm();
        if (!isRegistedFormClosed) {
            form.FormClosed += OnMainFormClosed;
            isRegistedFormClosed = true;
        }

        this.SuspendLayout();

        this.Dock = DockStyle.Fill;

        checkBox_AutoStart.Checked = isAutoStartTimer;
        checkBox_AutoReset.Checked = isAutoResetTimer;
        checkBox_Undo.Checked = isUndoSplit;

        toolTip.SetToolTip(checkBox_AutoStart, checkBox_AutoStart_TIP);
        toolTip.SetToolTip(checkBox_AutoReset, checkBox_AutoReset_TIP);
        toolTip.SetToolTip(checkBox_Undo, checkBox_Undo_TIP); 
        toolTip.SetToolTip(button_Reconnect, button_Reconnect_TIP); 
        
        for (int i = 0; i<Component.Run.Count; i++) {
            var segment = Component.Run[i];
            var split = (0<=i & i<SplitManager.SplitList.Count) 
                ? SplitManager.SplitList[i]
                : null;
            Debug.WriteLine($"[UI] Add split setting: {segment.Name} | {split?.FullName}");
            var frame =  new SplitSettingFrame(segment.Name, split);
            flow_SplitSettings.Controls.Add(frame);
            SplitSettingFrames.Add(frame);
        }

        this.ResumeLayout();
    }

    private void button_Connect_Click(object sender, EventArgs e)
    {
        CommunicationWrapper.ForceReconnect();
    }

    private void OnMainFormClosed(object sender, FormClosedEventArgs e)
    {
        Form form = FindForm();

        if (form.DialogResult == DialogResult.OK) {
            isAutoStartTimer = checkBox_AutoStart.Checked;
            isAutoResetTimer = checkBox_AutoReset.Checked;
            isUndoSplit = checkBox_Undo.Checked;

            SplitManager.Clear();
            SplitManager.AddSplits(SplitSettingFrames.Select(frame => frame.SplitSetting.Split));
        }
        
        flow_SplitSettings.SuspendLayout();
        foreach (var frame in SplitSettingFrames) {
            frame.Dispose();
        }
        SplitSettingFrames.Clear();
        flow_SplitSettings.Controls.Clear();
        flow_SplitSettings.ResumeLayout();
        Debug.WriteLine($"[UI] Clear all split settings");

        if (isRegistedFormClosed) {
            form.FormClosed -= OnMainFormClosed;
            isRegistedFormClosed = false;
        }
    }

    private void flow_SplitSettings_DragEnter(object sender, DragEventArgs e)
    {
        e.Effect = DragDropEffects.Move;
    }

    private void flow_SplitSettings_DragOver(object sender, DragEventArgs e)
    {
        var flow = (FlowLayoutPanel) sender;
        Point p = flow.PointToClient(new Point(e.X, e.Y));
        if (e.Data.GetData(typeof(Container<SplitSettingFrame>)) is Container<SplitSettingFrame> node
            && flow.GetChildAtPoint(p) is SplitSettingFrame dropping) {
            e.Effect = DragDropEffects.Move;
            var dragging = node.Value;
            if (dragging != dropping) {
                node.Value = dropping;
                dragging.SuspendLayout();
                dropping.SuspendLayout();
                var setting = dragging.SplitSetting;
                dragging.RemoveSplitSetting(false);
                dragging.AddSplitSetting(dropping.SplitSetting);
                dropping.RemoveSplitSetting(false);
                dropping.AddSplitSetting(setting);
                var type = dragging.lastSplitType;
                dragging.SetSplitType(dropping.lastSplitType);
                dropping.SetSplitType(type);
                dragging.ResumeLayout();
                dropping.ResumeLayout();
            }
        }
    }

    private void toolTip_Popup(object sender, PopupEventArgs e)
    {

    }
}