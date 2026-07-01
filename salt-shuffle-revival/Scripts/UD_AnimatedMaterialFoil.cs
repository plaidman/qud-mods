using System;

using ConsoleLib.Console;

using XRL.Core;
using XRL.Rules;
using XRL.UI;

namespace XRL.World.Parts {
    [Serializable]
    public class UD_AnimatedMaterialFoil : IScribedPart {
        public static int FrameMod = 240;
        
        public string OriginalColorString;

        public string OriginalTileColor;

        public string OriginalDetailColor;

        public int FrameOffset;

        public UD_AnimatedMaterialFoil() : base() { }

        public override void Initialize() {
            if (ParentObject.Render is Render render) {
                OriginalColorString ??= render.ColorString;
                OriginalTileColor ??= render.TileColor;
                OriginalDetailColor ??= render.DetailColor;
            }
            base.Initialize();
        }

        public override void Remove() {
            if (ParentObject.Render is Render render)
            {
                if (!OriginalTileColor.IsNullOrEmpty())
                    render.TileColor = OriginalTileColor;

                if (!OriginalColorString.IsNullOrEmpty())
                    render.ColorString = OriginalColorString;

                if (!OriginalDetailColor.IsNullOrEmpty())
                    render.DetailColor = OriginalDetailColor;
            }
            base.Remove();
        }

        public string GetOtherShade(string Color) {
            if (Color.IsNullOrEmpty())
                return "y";
            
            string strippedColor = Color.Replace("&", "");

            string bright = strippedColor.ToUpper();
            string dark = strippedColor.ToLower();

            return $"{(strippedColor != Color ? "&" : null)}{(strippedColor == bright ? dark : bright)}";
        }
        
        public string GetTileOtherShade()
            => GetOtherShade(GetOriginalTileColor())
            ;
        
        public string GetDetailOtherShade()
            => GetOtherShade(OriginalDetailColor)
            ;

        public string GetAlternateColor(string BasedOn, string Except = null) {
            string except = Except.Replace("&", "");
            if (BasedOn.ToUpper() != BasedOn && except != "Y")
                return "Y";
            
            return except != "y" ? "y" : "Y";
        }
        
        public string GetOriginalTileColor() {
            if (OriginalTileColor.IsNullOrEmpty())
                return ColorUtility.StripBackgroundFormatting(OriginalColorString);
            
            return OriginalTileColor;
        }

        public override bool Render(RenderEvent E) {
            if (ParentObject.Render is Render render) {
                int frame = (XRLCore.CurrentFrame + FrameOffset + (ParentObject?.BaseID ?? 0)) % FrameMod;

                string tileColor = null;
                string colorString = null;
                string detailColor = null;

                if (frame < 16) {
                    tileColor = GetTileOtherShade();
                    colorString = tileColor;
                    
                    detailColor = GetAlternateColor(BasedOn: OriginalDetailColor, Except: tileColor);
                    if (tileColor.Replace("&", "") == detailColor)
                        detailColor = GetOtherShade(detailColor);
                }
                else if (frame < 32) {
                    detailColor = GetDetailOtherShade();
                    
                    tileColor = $"&{GetAlternateColor(BasedOn: GetOriginalTileColor(), Except: detailColor)}";
                    if (tileColor.Replace("&", "") == detailColor)
                        tileColor = GetOtherShade(tileColor);
                    colorString = tileColor;
                }
                else if (frame < 64) {
                    detailColor = GetDetailOtherShade();
                    
                    tileColor = GetOriginalTileColor();
                    if (tileColor.Replace("&", "") == detailColor)
                        tileColor = GetOtherShade(tileColor);
                    colorString = tileColor;
                }
                else {
                    tileColor = GetOriginalTileColor();
                    colorString = tileColor;
                    
                    detailColor = OriginalDetailColor;
                    if (tileColor.Replace("&", "") == detailColor)
                        detailColor = GetOtherShade(detailColor);
                }

                if (!tileColor.IsNullOrEmpty())
                    render.TileColor = tileColor;

                if (!colorString.IsNullOrEmpty())
                    render.ColorString = colorString;

                if (!detailColor.IsNullOrEmpty())
                    render.DetailColor = detailColor;

                if (!Options.DisableTextAnimationEffects)
                    FrameOffset += Math.Max(0, Stat.RandomCosmetic(-2, 3));

                return true;
            }
            return base.Render(E);
        }
        
        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == GetDebugInternalsEvent.ID
            ;
            
            
        public override bool HandleEvent(GetDebugInternalsEvent E) {
            E.AddEntry(this, nameof(FrameOffset), FrameOffset);
            E.AddEntry(this, nameof(OriginalColorString), OriginalColorString);
            E.AddEntry(this, nameof(OriginalTileColor), OriginalTileColor);
            E.AddEntry(this, nameof(OriginalDetailColor), OriginalDetailColor);
            return base.HandleEvent(E);
        }
    }
}