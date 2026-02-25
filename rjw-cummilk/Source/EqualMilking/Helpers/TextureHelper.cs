using UnityEngine;
using Verse;

namespace EqualMilking.Helpers
{
	[StaticConstructorOnStartup]
	public static class TextureHelper
	{
        public static readonly Texture2D XenoBG = ContentFinder<Texture2D>.Get("ui/icons/genes/genebackground_xenogene", true);
        public static readonly Texture2D complexity = ContentFinder<Texture2D>.Get("ui/icons/biostats/complexity", true);
        public static readonly Texture2D metabolism = ContentFinder<Texture2D>.Get("ui/icons/biostats/metabolism", true);
        public static readonly Texture2D archite = ContentFinder<Texture2D>.Get("ui/icons/biostats/architecapsulerequired", true);
		public static readonly Texture2D milkBG = ContentFinder<Texture2D>.Get("UI/Icons/MilkGeneBG", true).Readable();
		public static Texture2D GenMilkGeneIcon(ThingDef thingDef)
		{
			Texture2D thingIcon = Widgets.GetIconFor(thingDef).Readable();
			Texture2D texture2D = new(milkBG.width, milkBG.height);
			thingIcon = Resize(thingIcon, milkBG.width / 2, milkBG.height / 2, true, FilterMode.Bilinear);
			texture2D.SetPixels(milkBG.GetPixels());
			for (int i = 0; i < thingIcon.width; i++)
			{
				for (int j = 0; j < thingIcon.height; j++)
				{
					Color color = thingIcon.GetPixel(i, j);
					if (color.a > 0f)
					{
						texture2D.SetPixel(i + milkBG.width / 4, j + milkBG.height / 4, thingIcon.GetPixel(i, j));
					}
				}
			}
			texture2D.Apply();
			return texture2D;
		}

		private static Texture2D Readable(this Texture2D texture)
		{
			RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
			Graphics.Blit(texture, temporary);
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = temporary;
			Texture2D texture2D = new(texture.width, texture.height);
			texture2D.ReadPixels(new Rect(0f, 0f, (float)temporary.width, (float)temporary.height), 0, 0);
			texture2D.Apply();
			RenderTexture.active = active;
			RenderTexture.ReleaseTemporary(temporary);
			return texture2D;
		}
		public static Texture2D Resize(Texture2D texture2D, int targetX, int targetY, bool mipmap = true, FilterMode filter = FilterMode.Bilinear)
		{
			RenderTexture temporary = RenderTexture.GetTemporary(targetX, targetY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
			RenderTexture.active = temporary;
			Graphics.Blit(texture2D, temporary);
			texture2D.ResizeTo(targetX, targetY, texture2D.format, mipmap);
			texture2D.filterMode = filter;
			try
			{
				texture2D.ReadPixels(new Rect(0f, 0f, (float)targetX, (float)targetY), 0, 0);
				texture2D.Apply();
			}
			catch
			{
				Log.Error("Read/Write is not enabled on texture " + texture2D.name);
			}
			RenderTexture.ReleaseTemporary(temporary);
			return texture2D;
		}
	}
}
