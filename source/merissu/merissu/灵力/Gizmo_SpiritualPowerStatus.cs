using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    [StaticConstructorOnStartup]
    public class Gizmo_SpiritualPowerStatus : Gizmo
    {
        private Pawn pawn;
        private Hediff hediff;

        private static readonly Texture2D Tex_Back = ContentFinder<Texture2D>.Get("UI/SpiritualPower_Back");
        private static readonly Texture2D Tex_Label = ContentFinder<Texture2D>.Get("UI/SpiritualPower_Label");
        private static readonly Texture2D Tex_Dot = ContentFinder<Texture2D>.Get("UI/Numbers/Dot");
        private static readonly Texture2D Tex_Slash = ContentFinder<Texture2D>.Get("UI/Numbers/Slash");
        private static readonly Texture2D[] Tex_Nums = new Texture2D[10];
        private static readonly Texture2D Tex_NewButton = ContentFinder<Texture2D>.Get("UI/BloodthirstyJade");
        private static readonly Texture2D Tex_ExtraButton = ContentFinder<Texture2D>.Get("UI/Transformspell");
        private static readonly Texture2D Tex_ThirdButton = ContentFinder<Texture2D>.Get("UI/DragonKhsier");
        private static readonly Texture2D Tex_FourthButton = ContentFinder<Texture2D>.Get("UI/Planahead");

        static Gizmo_SpiritualPowerStatus()
        {
            for (int i = 0; i < 10; i++)
            {
                Tex_Nums[i] = ContentFinder<Texture2D>.Get("UI/Numbers/" + i);
            }
        }

        public Gizmo_SpiritualPowerStatus(Pawn pawn, Hediff hediff)
        {
            this.pawn = pawn;
            this.hediff = hediff;
            this.Order = -120f;
        }

        public override float GetWidth(float maxWidth) => 300f;
        public override bool Visible
        {
            get
            {
                return Find.Selector.SelectedObjects.Count == 1;
            }
        }
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect baseRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            GUI.DrawTexture(baseRect, Tex_Back);

            float severity = hediff.Severity;
            float maxSeverity = hediff.def.maxSeverity;

            int total = Mathf.FloorToInt(severity * 100f);
            int cur_int = total / 100;
            int cur_ten = (total / 10) % 10;
            int cur_hun = total % 10;

            int maxTotal = Mathf.FloorToInt(maxSeverity * 100f);
            int m_int = maxTotal / 100;
            int m_ten = (maxTotal / 10) % 10;
            int m_hun = maxTotal % 10;

            float centerY = baseRect.y + baseRect.height / 2f;
            float curX = baseRect.x + 100f;

            float cIntW = 30f; float cIntH = 30f;
            float cDecW = 18f; float cDecH = 18f;
            float cIntY = 0f;
            float cDecY = 4f;
            float cGap = 0.75f;

            float mIntW = 30f; float mIntH = 30f;
            float mDecW = 18f; float mDecH = 18f;
            float mIntY = 0f;
            float mDecY = 4f;
            float mGap = 0.75f;

            float dotW = 10f; float dotH = 10f; float dotY = 8f;
            float slsW = 30f; float slsH = 30f; float slsY = 0f;

            GUI.DrawTexture(new Rect(baseRect.x + 12f, centerY - 48f, 95f, 95f), Tex_Label);

            DrawCustom(ref curX, centerY + cIntY, Tex_Nums[cur_int], cIntW, cIntH, cGap);
            DrawCustom(ref curX, centerY + dotY, Tex_Dot, dotW, dotH, 0.5f);
            DrawCustom(ref curX, centerY + cDecY, Tex_Nums[cur_ten], cDecW, cDecH, cGap);
            DrawCustom(ref curX, centerY + cDecY, Tex_Nums[cur_hun], cDecW, cDecH, 0.6f);

            DrawCustom(ref curX, centerY + slsY, Tex_Slash, slsW, slsH, 0.4f);
            curX += 4f;

            DrawCustom(ref curX, centerY + mIntY, Tex_Nums[m_int], mIntW, mIntH, mGap);
            DrawCustom(ref curX, centerY + dotY, Tex_Dot, dotW, dotH, 0.5f);
            DrawCustom(ref curX, centerY + mDecY, Tex_Nums[m_ten], mDecW, mDecH, mGap);
            DrawCustom(ref curX, centerY + mDecY, Tex_Nums[m_hun], mDecW, mDecH, mGap);

            float btnSize = 33f;
            float spacingX = -3f;
            float spacingY = 2f;
            float offsetRight = 4f;
            float offsetTop = 4f;

            Rect btnRect2 = new Rect(baseRect.xMax - btnSize - offsetRight, baseRect.y + offsetTop, btnSize, btnSize);
            TooltipHandler.TipRegion(btnRect2, "SpiritualPower_ExtraButton_Tip".Translate());
            if (Widgets.ButtonImage(btnRect2, Tex_ExtraButton))
            {
                OnExtraButtonClicked();
            }

            Rect btnRect4 = new Rect(btnRect2.x, btnRect2.yMax + spacingY, btnSize, btnSize);
            TooltipHandler.TipRegion(btnRect4, "SpiritualPower_FourthButton_Tip".Translate());
            if (Widgets.ButtonImage(btnRect4, Tex_FourthButton))
            {
                OnFourthButtonClicked();
            }

            float colLeftX = btnRect2.x - btnSize - spacingX;
            Rect btnRect1 = new Rect(colLeftX, baseRect.y + offsetTop, btnSize, btnSize);
            TooltipHandler.TipRegion(btnRect1, "SpiritualPower_CollectButton_Tip".Translate());
            if (Widgets.ButtonImage(btnRect1, Tex_NewButton))
            {
                OnButtonClicked();
            }

            Rect btnRect3 = new Rect(colLeftX, btnRect1.yMax + spacingY, btnSize, btnSize);
            TooltipHandler.TipRegion(btnRect3, "SpiritualPower_ThirdButton_Tip".Translate());
            if (Widgets.ButtonImage(btnRect3, Tex_ThirdButton))
            {
                OnThirdButtonClicked();
            }

            return new GizmoResult(GizmoState.Clear);
        }

        private void DrawCustom(ref float x, float drawY, Texture2D tex, float w, float h, float gap)
        {
            if (tex == null) return;
            Rect rect = new Rect(x, drawY - (h / 2f), w, h);
            GUI.DrawTexture(rect, tex);
            x += (w * gap);
        }

        public void OnButtonClicked()
        {
            Map map = pawn.Map;
            if (map == null) return;
            float gap = hediff.def.maxSeverity - hediff.Severity;
            int needCount = Mathf.CeilToInt(gap * 100f);

            if (needCount <= 0)
            {
                Messages.Message("灵力已满，无需收取", MessageTypeDefOf.RejectInput, false);
                return;
            }

            ThingDef powerPointDef = ThingDef.Named("PowerPoint");
            List<Thing> allPowerPoints = map.listerThings.ThingsOfDef(powerPointDef);

            if (allPowerPoints.NullOrEmpty())
            {
                Messages.Message("地图上没有可收取的P点。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int spawnedCount = 0;
            List<Thing> targets = new List<Thing>(allPowerPoints);
            foreach (Thing t in targets)
            {
                if (spawnedCount >= needCount) break;
                if (t.Destroyed || t.IsForbidden(pawn)) continue;
                int takeCount = Mathf.Min(t.stackCount, needCount - spawnedCount);
                SpawnFlyingPower(t, pawn, takeCount);
                spawnedCount += takeCount;
                if (takeCount >= t.stackCount) t.Destroy();
                else t.stackCount -= takeCount;
            }
            if (spawnedCount > 0)
            {
                Messages.Message($"正在收取 {spawnedCount} P点...", MessageTypeDefOf.SilentInput, false);
            }
        }

        private void OnExtraButtonClicked()
        {
            if (hediff.Severity < 0.75f)
            {
                Messages.Message("灵力不足 (需要 0.75)", MessageTypeDefOf.RejectInput, false);
                return;
            }
            hediff.Severity -= 0.75f;
            HediffDef fullPowerDef = HediffDef.Named("FullPower");
            Hediff existingFullPower = pawn.health.hediffSet.GetFirstHediffOfDef(fullPowerDef);
            if (existingFullPower != null) existingFullPower.Severity += 1f;
            else
            {
                Hediff newHediff = HediffMaker.MakeHediff(fullPowerDef, pawn);
                newHediff.Severity = 1f;
                pawn.health.AddHediff(newHediff);
            }
            SoundDef.Named("powerup").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
        }

        private void OnThirdButtonClicked()
        {
            if (hediff.Severity < 0.75f)
            {
                Messages.Message("灵力不足 (需要 0.75)", MessageTypeDefOf.RejectInput, false);
                return;
            }
            hediff.Severity -= 0.75f;
            HediffDef upDef = HediffDef.Named("up");
            Hediff existingUp = pawn.health.hediffSet.GetFirstHediffOfDef(upDef);
            if (existingUp != null) existingUp.Severity += 1f;
            else
            {
                Hediff newHediff = HediffMaker.MakeHediff(upDef, pawn);
                newHediff.Severity = 1f;
                pawn.health.AddHediff(newHediff);
            }
            SoundDef.Named("up").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
        }

        private void OnFourthButtonClicked()
        {
            if (hediff.Severity < 0.5f)
            {
                Messages.Message("灵力不足 (需要 0.5)", MessageTypeDefOf.RejectInput, false);
                return;
            }

            hediff.Severity -= 0.5f;

            Thing thing = ThingMaker.MakeThing(ThingDef.Named("FullPower"));
            GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);

            SoundDef.Named("power").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

            Messages.Message("灵力凝聚成了满p", MessageTypeDefOf.PositiveEvent, false);
        }

        private void SpawnFlyingPower(Thing source, Pawn targetPawn, int count)
        {
            PowerProjectile flyer = (PowerProjectile)GenSpawn.Spawn(ThingDef.Named("FlyingPowerPoint"), source.Position, source.Map);
            flyer.Launch(source, source.DrawPos, targetPawn, targetPawn, ProjectileHitFlags.IntendedTarget);
            flyer.powerCount = count;
        }
    }

    public class PowerProjectile : Projectile
    {
        public int powerCount = 1;
        private int updateTicks = 0;
        private const int UpdateRate = 4;

        protected override void Tick()
        {
            Pawn victim = this.intendedTarget.Thing as Pawn;
            if (victim == null || victim.Destroyed || victim.Dead)
            {
                this.Destroy();
                return;
            }

            updateTicks++;
            if (updateTicks >= UpdateRate)
            {
                this.destination = victim.DrawPos;
                this.Position = victim.Position;
                updateTicks = 0; 
            }

            Vector3 currentPos = this.DrawPos;
            Vector3 targetPos = victim.DrawPos;

            if (Vector3.Distance(currentPos, targetPos) < 0.2f)
            {
                this.Impact(victim);
                return;
            }

            base.Tick();
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Pawn victim = hitThing as Pawn ?? this.intendedTarget.Thing as Pawn;
            if (victim == null) victim = this.Position.GetFirstPawn(this.Map);

            if (victim != null && !victim.Dead)
            {
                Hediff scHediff = victim.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("spiritualpower"));
                if (scHediff != null)
                {
                    scHediff.Severity += (0.01f * powerCount);
                    SoundDef.Named("se_graze").PlayOneShot(new TargetInfo(victim.Position, victim.Map));
                }
            }
            this.Destroy();
        }
    }
    public class HediffComp_SpiritualPowerStatus : HediffComp
    {
        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            yield return new Gizmo_SpiritualPowerStatus(Pawn, parent);
        }
    }

    public class HediffCompProperties_SpiritualPowerStatus : HediffCompProperties
    {
        public HediffCompProperties_SpiritualPowerStatus()
        {
            this.compClass = typeof(HediffComp_SpiritualPowerStatus);
        }
    }
}