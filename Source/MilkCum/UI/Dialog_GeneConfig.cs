using System;
using MilkCum.Milk.Helpers;
using MilkCum.Milk.Data;
using MilkCum.Milk.Helpers;
using UnityEngine;
using Verse;

namespace MilkCum.UI;
public class Dialog_GeneConfig : Window
{
    private readonly Gene_MilkTypeData gene_MilkType;
    private readonly Action<Gene_MilkTypeData> onConfirm;
    private static readonly Vector2 ButtonSize = new(120f, 32f);
    public override Vector2 InitialSize => new(280f, 450f);
    public Dialog_GeneConfig(Action<Gene_MilkTypeData> onConfirm, Gene_MilkTypeData gene_MilkType = null)
    {
        this.onConfirm = onConfirm;
        this.draggable = true;
        this.closeOnAccept = false;
        this.closeOnClickedOutside = true;
        this.absorbInputAroundWindow = true;
        this.gene_MilkType = new Gene_MilkTypeData();
        if (gene_MilkType != null)
        {
            this.gene_MilkType.CopyFrom(gene_MilkType);
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        bool isEnterPressed = false;
        if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
        {
            isEnterPressed = true;
            Event.current.Use();
        }
        // Draw Gene Icon
        Rect iconRect = new(inRect.x, inRect.y + 10f, 128f, 128f);
        Widgets.DrawTextureFitted(iconRect, TextureHelper.XenoBG, 1f);
        Widgets.DrawTextureFitted(iconRect, TextureHelper.milkBG, 1f);
        if (this.gene_MilkType.ThingDef != null)
        {
            Widgets.ThingIcon(iconRect.ContractedBy(iconRect.width / 4f), this.gene_MilkType.ThingDef);
        }

        //Thing button
        Rect iconSideRect = new(inRect.x + iconRect.width, inRect.y + 10f, inRect.width - iconRect.width, ButtonSize.y);
        string thingButtonText = this.gene_MilkType.ThingDef?.LabelCap ?? Lang.ClickToSelect;
        if (Widgets.ButtonText(iconSideRect, thingButtonText))
        {
            Find.WindowStack.Add(new Window_Search(gene_MilkType.SetMilkType) { windowRect = new Rect(iconSideRect.x, iconSideRect.y, iconSideRect.width, 500) });
        }

        //Complexity
        iconSideRect.y += ButtonSize.y;
        Widgets.DrawTextureFitted(iconSideRect.LeftPartPixels(ButtonSize.y), TextureHelper.complexity, 1f);
        string cpxBuffer = this.gene_MilkType.biostatCpx.ToString();
        Widgets.TextFieldNumeric<int>(iconSideRect.RightPartPixels(iconSideRect.width - ButtonSize.y), ref this.gene_MilkType.biostatCpx, ref cpxBuffer, -50, 50);

        //Metabolism
        iconSideRect.y += ButtonSize.y;
        Widgets.DrawTextureFitted(iconSideRect.LeftPartPixels(ButtonSize.y), TextureHelper.metabolism, 1f);
        string metBuffer = this.gene_MilkType.biostatMet.ToString();
        Widgets.TextFieldNumeric<int>(iconSideRect.RightPartPixels(iconSideRect.width - ButtonSize.y), ref this.gene_MilkType.biostatMet, ref metBuffer, -50, 50);

        //Archite
        iconSideRect.y += ButtonSize.y;
        Widgets.DrawTextureFitted(iconSideRect.LeftPartPixels(ButtonSize.y), TextureHelper.archite, 1f);
        string arcBuffer = this.gene_MilkType.biostatArc.ToString();
        Widgets.TextFieldNumeric<int>(iconSideRect.RightPartPixels(iconSideRect.width - ButtonSize.y), ref this.gene_MilkType.biostatArc, ref arcBuffer, 0, 100);

        //Stat Factor
        Rect lineRect = new(inRect.x, iconSideRect.yMax, inRect.width, ButtonSize.y);
        Widgets.Label(lineRect, Lang.MilkAmount);
        lineRect.y += ButtonSize.y;
        Widgets.HorizontalSlider(lineRect, ref this.gene_MilkType.milkAmountOffset, new FloatRange(-5f, 5f), (this.gene_MilkType.milkAmountOffset >= 0f ? "+" : "") + this.gene_MilkType.milkAmountOffset.ToStringByStyle(ToStringStyle.PercentOne));
        lineRect.y += ButtonSize.y;
        Widgets.HorizontalSlider(lineRect, ref this.gene_MilkType.milkAmountFactor, new FloatRange(0f, 5f), "x" + this.gene_MilkType.milkAmountFactor.ToStringByStyle(ToStringStyle.PercentZero), 0.01f);
        lineRect.y += ButtonSize.y;
        Widgets.Label(lineRect, Lang.Join(Lang.Lactating, Lang.Efficiency));
        lineRect.y += ButtonSize.y;
        Widgets.HorizontalSlider(lineRect, ref this.gene_MilkType.milkEfficiencyOffset, new FloatRange(-5f, 5f), (this.gene_MilkType.milkEfficiencyOffset >= 0f ? "+" : "") + this.gene_MilkType.milkEfficiencyOffset.ToStringByStyle(ToStringStyle.PercentOne));
        lineRect.y += ButtonSize.y;
        Widgets.HorizontalSlider(lineRect, ref this.gene_MilkType.milkEfficiencyFactor, new FloatRange(0f, 5f), "x" + this.gene_MilkType.milkEfficiencyFactor.ToStringByStyle(ToStringStyle.PercentZero), 0.01f);


        //Cancel button
        Rect cancelRect = inRect;
        cancelRect.width = inRect.width / 2f - 5f;
        cancelRect.yMin = inRect.yMax - ButtonSize.y - 10f;
        if (Widgets.ButtonText(cancelRect, Lang.Cancel))
        {
            Find.WindowStack.TryRemove(this);
        }

        //Confirm button
        Rect confirmRect = inRect;
        confirmRect.xMin = cancelRect.xMax + 10f;
        confirmRect.yMin = inRect.yMax - ButtonSize.y - 10f;
        if (Widgets.ButtonText(confirmRect, Lang.Confirm) || isEnterPressed)
        {
            onConfirm?.Invoke(gene_MilkType);
            Find.WindowStack.TryRemove(this);
        }
    }
}