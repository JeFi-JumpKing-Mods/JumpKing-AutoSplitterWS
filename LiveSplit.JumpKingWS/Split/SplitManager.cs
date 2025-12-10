using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using LiveSplit.JumpKingWS.UI;

namespace LiveSplit.JumpKingWS.Split;

public static class SplitManager
{
    public readonly static List<SplitBase> SplitList;
    private static (int, SplitBase)? undoSplit;
    private static int CurrentIndex => Component.State?.CurrentSplitIndex ?? -1;

    static SplitManager()
    {
        SplitList = [];
        undoSplit = null;
    }

    public static void Clear()
    {
        SplitList.Clear(); 
        undoSplit = null;
    }
    public static void AddSplits(IEnumerable<SplitBase> splitList)
    {
        SplitList.AddRange(splitList);
    }

    public static void SetUndoSplit(int index, SplitBase split)
    {
        if (!Settings.isUndoSplit) return;

        Debug.WriteLine($"[Split] Add undoSplit {split.SplitType} at {index}");
        if (undoSplit==null) {
            undoSplit = (index, split);
        }
    }
    public static void RemoveUndoSplit()
    {
        Debug.WriteLine($"[Split] Remove undoSplit");
        undoSplit = null;
    }

    public static void LoadFromXml(XmlNode splitsNode)
    {
        Clear();
        if (splitsNode==null) return;
        
        foreach (XmlNode node in splitsNode.SelectNodes(".//Split"))
        {
            try
            {
                switch(node.Attributes["type"]?.Value) 
                {
                    case "Manual":
                        SplitList.Add(new ManualSplit(node));
                        break;
                    case "Screen":
                        SplitList.Add(new ScreenSplit(node));
                        break;
                    case "Item":
                        SplitList.Add(new ItemSplit(node));
                        break;
                    case "Raven":
                        SplitList.Add(new RavenSplit(node));
                        break;
                    case "Achievement":
                        SplitList.Add(new AchievementSplit(node));
                        break;
                    case "Ending":
                        SplitList.Add(new EndingSplit(node));
                        break;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
    public static XmlElement GetXmlElement(XmlDocument document)
    {
        XmlElement splits = document.CreateElement("Splits");

        // add offset to make reading easier
        int offset = 0;
        foreach (SplitBase split in SplitList)
        {
            XmlElement splitElement = split.GetXmlElement(document);
            splitElement.SetAttribute("offset", offset.ToString());
            splits.AppendChild(splitElement);
            offset++;
        }

        return splits;
    }

    public static void UpdatSplits()
    {
        int lastIndex, undoIndex;
        SplitBase split;
        bool isSkip = false;
        while (0<=CurrentIndex && CurrentIndex<SplitList.Count)
        {
            lastIndex = CurrentIndex;
            split = SplitList[CurrentIndex];
            if (split.CheckSplit()) {
                if (!isSkip || CurrentIndex == Component.Run.Count-1) {
                    isSkip = true;
                    Component.Timer.Split();
                } else {
                    Component.Timer.SkipSplit();
                }
            }

            if (CurrentIndex==lastIndex) {
                break;
            } else {
                split.OnSplit(lastIndex);
            }
        }

        if (undoSplit!=null) {
            (undoIndex, split) = undoSplit.Value;
            switch (split.CheckUndo())
            {
                case UndoResult.Skip:
                    break;
                case UndoResult.Undo:
                    while (CurrentIndex>undoIndex) {
                        lastIndex = CurrentIndex;
                        Component.Timer.UndoSplit();
                        if (CurrentIndex==lastIndex) break;
                    }
                    if (CurrentIndex==undoIndex) {
                        RemoveUndoSplit();
                    }
                    break;
                case UndoResult.Remove:
                    RemoveUndoSplit();
                    break;
            }
        }
    }

    public static int GetHash()
    {
        const int Int32BitSize = 32;
        int hash = 0;
        int shift = 0;
        int quotient = 0;
        int h;
        foreach (var split in SplitList) {
            h = split.GetHash()+quotient;
            h = (h<<shift)|(h>>(Int32BitSize-shift));
            hash ^= h;
            shift++;
            if (shift>=Int32BitSize) {
                shift -= Int32BitSize;
                quotient++;
            }
        }
        return hash;
    }
}