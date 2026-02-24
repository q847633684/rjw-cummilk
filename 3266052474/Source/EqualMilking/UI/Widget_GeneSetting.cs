using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using EqualMilking.Helpers;
using EqualMilking.Data;

namespace EqualMilking.UI;
[StaticConstructorOnStartup]
public class Widget_GeneSetting
{
    private readonly List<Gene_MilkTypeData> gene_MilkTypes;
    private const float LINE_HEIGHT = 45f;
    private const float ELEMENT_HEIGHT = 30f;
    private Vector2 scrollPosition = Vector2.zero;
    public Widget_GeneSetting(List<Gene_MilkTypeData> gene_MilkTypes)
    {
        this.gene_MilkTypes = gene_MilkTypes;
    }
    public void Draw(Rect inRect)
    {
        Rect scrollViewRect = new(inRect.x, inRect.y, inRect.width, inRect.height - ELEMENT_HEIGHT);
        Rect viewRect = new(0f, 0f, inRect.width - 16f, gene_MilkTypes.Count * LINE_HEIGHT);

        Widgets.BeginScrollView(scrollViewRect, ref scrollPosition, viewRect);

        for (int i = 0; i < gene_MilkTypes.Count; i++)
        {
            Rect rowRect = new(0f, i * LINE_HEIGHT, viewRect.width, ELEMENT_HEIGHT);

            Gene_MilkTypeData geneData = gene_MilkTypes[i];

            // Display deName
            Rect defNameRect = new(rowRect.x, rowRect.y, 200f, LINE_HEIGHT);
            Widgets.DrawTextureFitted(defNameRect.LeftPartPixels(LINE_HEIGHT), TextureHelper.XenoBG, 1f);
            Widgets.DrawTextureFitted(defNameRect.LeftPartPixels(LINE_HEIGHT), TextureHelper.milkBG, 1f);
            Widgets.ThingIcon(defNameRect.LeftPartPixels(LINE_HEIGHT).ContractedBy(LINE_HEIGHT / 4f), geneData.ThingDef);
            Widgets.Label(defNameRect.RightPartPixels(defNameRect.width - LINE_HEIGHT), geneData.ThingDef?.LabelCap ?? "???" + "(" + geneData.thingDefName + ")");

            //Complexity
            rowRect.x += 200f;
            rowRect.width = ELEMENT_HEIGHT;
            Widgets.DrawTextureFitted(rowRect, TextureHelper.complexity, 1f);
            rowRect.x += ELEMENT_HEIGHT;
            Widgets.Label(rowRect, geneData.biostatCpx.ToString());
            rowRect.x += ELEMENT_HEIGHT;

            //Metabolism
            Widgets.DrawTextureFitted(rowRect, TextureHelper.metabolism, 1f);
            rowRect.x += ELEMENT_HEIGHT;
            Widgets.Label(rowRect, geneData.biostatMet.ToString());
            rowRect.x += ELEMENT_HEIGHT;

            //Archite
            Widgets.DrawTextureFitted(rowRect, TextureHelper.archite, 1f);
            rowRect.x += ELEMENT_HEIGHT;
            Widgets.Label(rowRect, geneData.biostatArc.ToString());
            rowRect.x += ELEMENT_HEIGHT;
            //Stats
            rowRect.width = 300f;
            rowRect.height = LINE_HEIGHT;
            Widgets.Label(rowRect, geneData.StatSummary());
            rowRect.height = ELEMENT_HEIGHT;
            // Edit button
            Rect editButtonRect = new(viewRect.width - (2f * ELEMENT_HEIGHT), rowRect.y, rowRect.height, rowRect.height);
            if (Widgets.ButtonImage(editButtonRect, TexButton.Info))
            {
                Find.WindowStack.Add(new Dialog_GeneConfig(geneData.CopyFrom, geneData));
            }
            // Display delete button
            Rect deleteButtonRect = new(viewRect.width - ELEMENT_HEIGHT, rowRect.y, rowRect.height, rowRect.height);
            if (Widgets.ButtonImage(deleteButtonRect, TexButton.Delete))
            {
                gene_MilkTypes.RemoveAt(i);
                i--;
            }
        }
        Widgets.EndScrollView();

        // Add new entry button
        Rect addButtonRect = new(inRect.x, inRect.yMax - ELEMENT_HEIGHT, inRect.width, ELEMENT_HEIGHT);
        if (Widgets.ButtonText(addButtonRect, "+"))
        {
            Find.WindowStack.Add(new Dialog_GeneConfig(AddGene));
        }
    }
    private void AddGene(Gene_MilkTypeData geneData)
    {
        if (geneData.ThingDef == null) { return; }
        if (this.gene_MilkTypes.Where(x => x.ThingDef == geneData.ThingDef).FirstOrDefault() is Gene_MilkTypeData existingData)
        {
            existingData.CopyFrom(geneData);
            return;
        }
        this.gene_MilkTypes.Add(geneData);
    }
}
