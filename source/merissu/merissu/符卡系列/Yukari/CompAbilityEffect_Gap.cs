using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace merissu
{
    public class CompAbilityEffect_Gap : CompAbilityEffect_WithDest
    {
        public static string SkipUsedSignalTag = "CompAbilityEffect.SkipUsed";
        public new CompProperties_AbilityTeleport Props => (CompProperties_AbilityTeleport)props;

        private static readonly SoundDef GapOneSound = SoundDef.Named("gapone");
        private static readonly SoundDef GapTwoSound = SoundDef.Named("gaptwo");
        private static readonly SoundDef GapKillSound = SoundDef.Named("gapkill");

        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo t, LocalTargetInfo d)
                {
                    Map map = parent.pawn.Map;
                    GapOneSound?.PlayOneShot(new TargetInfo(t.Cell, map));

                    if (t == d)
                    {
                        if (!parent.def.HasAreaOfEffect)
                        {
                            Pawn pawn = t.Pawn;
                            if (pawn != null)
                            {
                                FleckCreationData dataAttachedOverlay =
                                    FleckMaker.GetDataAttachedOverlay(pawn, FleckDefOf.PsycastSkipFlashEntry,
                                        new Vector3(-0.5f, 0f, -0.5f));

                                dataAttachedOverlay.link.detachAfterTicks = 5;
                                pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
                            }
                            else
                            {
                                FleckMaker.Static(t.CenterVector3, map, FleckDefOf.PsycastSkipFlashEntry);
                            }
                            FleckMaker.Static(d.Cell, map, FleckDefOf.PsycastSkipInnerExit);
                        }

                        if (Props.destination != AbilityEffectDestination.RandomInRange)
                        {
                            FleckMaker.Static(d.Cell, map, FleckDefOf.PsycastSkipOuterRingExit);
                        }
                    }
                },
                ticksAwayFromCast = 5
            };
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!target.HasThing)
                return;

            base.Apply(target, dest);

            Pawn pawn = target.Pawn;
            Map map = parent.pawn.Map;

            if (target == dest)
            {
                GapKillSound?.PlayOneShot(new TargetInfo(target.Cell, map));
                FleckMaker.ThrowDustPuff(target.Cell, map, 2f);

                Thing targetThing = target.Thing;

                targetThing.TryGetComp<CompCanBeDormant>()?.WakeUp();

                Thing_GapKiller killer = (Thing_GapKiller)ThingMaker.MakeThing(ThingDef.Named("GapKiller"));
                killer.isPawn = pawn != null;

                GenSpawn.Spawn(killer, targetThing.Position, map);

                targetThing.DeSpawn();
                killer.innerContainer.TryAdd(targetThing);

                killer.CacheTexture();

                return;
            }

            GapTwoSound?.PlayOneShot(new TargetInfo(dest.Cell, map));

            LocalTargetInfo destination = GetDestination(dest.IsValid ? dest : target);
            if (!destination.IsValid)
                return;

            Thing tThing = target.Thing;
            tThing.TryGetComp<CompCanBeDormant>()?.WakeUp();

            Thing_GapTeleporter teleporter = (Thing_GapTeleporter)ThingMaker.MakeThing(ThingDef.Named("GapTeleporter"));
            teleporter.destCell = destination.Cell;
            teleporter.caster = parent.pawn;
            teleporter.stunTicks = Props.stunTicks.RandomInRange;
            teleporter.destClamorType = Props.destClamorType;
            teleporter.destClamorRadius = Props.destClamorRadius;
            teleporter.isPawn = pawn != null;

            GenSpawn.Spawn(teleporter, tThing.Position, map);

            tThing.DeSpawn();
            teleporter.innerContainer.TryAdd(tThing);
            teleporter.CacheTexture();
        }
    }

    public class Thing_GapKiller : Thing, IThingHolder
    {
        public ThingOwner innerContainer;
        public int age = 0;
        public bool isPawn;

        private Material cachedThingMat;
        private Vector2 thingDrawSize = Vector2.one;
        private RenderTexture cachedRT;

        private const int MaxAge = 25;

        public Thing_GapKiller()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref age, "age");
            Scribe_Values.Look(ref isPawn, "isPawn");
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;
        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            CacheTexture();
        }

        public void CacheTexture()
        {
            if (innerContainer.Count == 0)
                return;

            if (cachedRT != null)
            {
                cachedRT.Release();
                cachedRT = null;
            }

            Thing t = innerContainer[0];

            if (t is Pawn p)
            {
                RenderTexture rt = new RenderTexture(256, 512, 24, RenderTextureFormat.ARGB32);
                rt.Create();
                Find.PawnCacheRenderer.RenderPawn(
                    p, rt, cameraOffset: default(Vector3), cameraZoom: 1.0f, angle: 0f, rotation: Rot4.South,
                    renderHead: true, renderHeadgear: true, renderClothes: true, portrait: false
                );

                cachedThingMat = MaterialPool.MatFrom(new MaterialRequest
                {
                    mainTex = rt,
                    shader = ShaderDatabase.Transparent,
                    color = Color.white,
                    colorTwo = Color.white,
                    renderQueue = 3500
                });
                thingDrawSize = new Vector2(1.0f, 2.0f);
                cachedRT = rt;
            }
            else if (t is Corpse corpse)
            {
                Pawn inner = corpse.InnerPawn;
                RenderTexture rt = new RenderTexture(256, 512, 24, RenderTextureFormat.ARGB32);
                rt.Create();
                Find.PawnCacheRenderer.RenderPawn(
                    inner, rt, cameraOffset: default(Vector3), cameraZoom: 1.0f, angle: 0f, rotation: Rot4.South,
                    renderHead: true, renderHeadgear: true, renderClothes: true, portrait: false
                );

                cachedThingMat = MaterialPool.MatFrom(new MaterialRequest
                {
                    mainTex = rt,
                    shader = ShaderDatabase.Transparent,
                    color = Color.white,
                    colorTwo = Color.white,
                    renderQueue = 3500
                });
                thingDrawSize = new Vector2(1.0f, 2.0f);
                cachedRT = rt;
            }
            else
            {
                cachedThingMat = t.Graphic.MatSingleFor(t);
                thingDrawSize = t.Graphic.drawSize;
                cachedRT = null;
            }
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= MaxAge)
            {
                FinishKill();
            }
        }

        private void FinishKill()
        {
            Pawn pawnToRemove = null;

            if (innerContainer.Count > 0)
            {
                Thing t = innerContainer[0];
                if (t is Pawn p)
                {
                    pawnToRemove = p;
                }
                else if (t is Corpse c)
                {
                    pawnToRemove = c.InnerPawn;
                }
            }

            innerContainer.ClearAndDestroyContents();

            if (pawnToRemove != null)
            {
                Find.WorldPawns.RemovePawn(pawnToRemove);
            }

            if (cachedRT != null)
            {
                cachedRT.Release();
                cachedRT = null;
            }

            this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (age >= MaxAge || !this.Spawned) return;

            if (cachedThingMat == null && innerContainer.Count > 0)
                CacheTexture();

            float progress = (float)age / MaxAge;

            int frame = (age / 1) % 13;

            Vector3 basePos = this.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead);
            Vector3 gapPos = basePos;
            gapPos.y += 1f;
            gapPos.y -= 0.1f;
            float gapCurrentWidth;

            if (progress < 0.5f)
            {
                gapCurrentWidth = Mathf.Lerp(0f, 2f, progress * 2f);
            }
            else
            {
                gapCurrentWidth = Mathf.Lerp(2f, 0f, (progress - 0.5f) * 2f);
            }
            Material gapMat = MaterialPool.MatFrom($"Other/gap/gapkill/bulletEa{frame:D3}", ShaderDatabase.Mote);

            Matrix4x4 gapMatrix = Matrix4x4.TRS(
                gapPos,
                Quaternion.identity,
                new Vector3(gapCurrentWidth, 2f, 4f)
            );
            Graphics.DrawMesh(MeshPool.plane10, gapMatrix, gapMat, 0);

            if (cachedThingMat != null)
            {
                Vector3 thingPos = basePos;
                thingPos.y += 0.05f;
                float thingProgress = 0f;

                if (progress > 0.5f)
                {
                    thingProgress = (progress - 0.5f) * 2f;
                }

                float thingCurrentWidth = Mathf.Lerp(
                    thingDrawSize.x,
                    0f,
                    thingProgress
                );
                Matrix4x4 thingMatrix = Matrix4x4.TRS(
                    thingPos,
                    Quaternion.identity,
                    new Vector3(thingCurrentWidth, 1f, thingDrawSize.y)
                );
                Graphics.DrawMesh(MeshPool.plane10, thingMatrix, cachedThingMat, 0);
            }
        }
    }

    public class Thing_GapTeleporter : Thing, IThingHolder
    {
        public IntVec3 destCell;
        public ThingOwner innerContainer;
        public int age = 0;
        public Pawn caster;
        public int stunTicks;
        public ClamorDef destClamorType;
        public float destClamorRadius;
        public bool isPawn;

        private Material cachedThingMat;
        private Vector2 thingDrawSize = Vector2.one;
        private RenderTexture cachedRT;

        private Mesh sinkMesh;
        private Mesh riseMesh;

        public Thing_GapTeleporter()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref destCell, "destCell");
            Scribe_Values.Look(ref age, "age");
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref stunTicks, "stunTicks");
            Scribe_Defs.Look(ref destClamorType, "destClamorType");
            Scribe_Values.Look(ref destClamorRadius, "destClamorRadius");
            Scribe_Values.Look(ref isPawn, "isPawn");
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;
        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            CacheTexture();
        }

        public void CacheTexture()
        {
            if (innerContainer.Count == 0)
                return;

            if (cachedRT != null)
            {
                cachedRT.Release();
                cachedRT = null;
            }

            Thing t = innerContainer[0];

            if (t is Pawn p)
            {
                RenderTexture rt = new RenderTexture(256, 512, 24, RenderTextureFormat.ARGB32);
                rt.Create();
                Find.PawnCacheRenderer.RenderPawn(
                    p,
                    rt,
                    cameraOffset: default(Vector3),
                    cameraZoom: 1.0f,
                    angle: 0f,
                    rotation: Rot4.South,
                    renderHead: true,
                    renderHeadgear: true,
                    renderClothes: true,
                    portrait: false
                );

                cachedThingMat = MaterialPool.MatFrom(new MaterialRequest
                {
                    mainTex = rt,
                    shader = ShaderDatabase.Transparent,
                    color = Color.white,
                    colorTwo = Color.white,
                    renderQueue = 3500
                });
                thingDrawSize = new Vector2(1.0f, 2.0f);
                cachedRT = rt;
            }
            else if (t is Corpse corpse)
            {
                Pawn inner = corpse.InnerPawn;
                RenderTexture rt = new RenderTexture(256, 512, 24, RenderTextureFormat.ARGB32);
                rt.Create();
                Find.PawnCacheRenderer.RenderPawn(
                    inner,
                    rt,
                    cameraOffset: default(Vector3),
                    cameraZoom: 1.0f,
                    angle: 0f,
                    rotation: Rot4.South,
                    renderHead: true,
                    renderHeadgear: true,
                    renderClothes: true,
                    portrait: false
                );

                cachedThingMat = MaterialPool.MatFrom(new MaterialRequest
                {
                    mainTex = rt,
                    shader = ShaderDatabase.Transparent,
                    color = Color.white,
                    colorTwo = Color.white,
                    renderQueue = 3500
                });
                thingDrawSize = new Vector2(1.0f, 2.0f);
                cachedRT = rt;
            }
            else
            {
                cachedThingMat = t.Graphic.MatSingleFor(t);
                thingDrawSize = t.Graphic.drawSize;
                cachedRT = null;
            }
        }

        protected override void Tick()
        {
            base.Tick();
            age++;
            if (age >= 30)
            {
                FinishTeleport();
            }
        }

        private void FinishTeleport()
        {
            if (innerContainer.Count > 0)
            {
                Thing t = innerContainer[0];

                innerContainer.TryDrop(t, destCell, this.Map, ThingPlaceMode.Direct, out Thing resultingThing);

                if (t is Pawn transportedPawn)
                {
                    if ((transportedPawn.Faction == Faction.OfPlayer || transportedPawn.IsPlayerControlled) && transportedPawn.Position.Fogged(this.Map))
                    {
                        FloodFillerFog.FloodUnfog(transportedPawn.Position, this.Map);
                    }

                    if (stunTicks > 0)
                    {
                        transportedPawn.stances.stunner.StunFor(stunTicks, caster, addBattleLog: false, showMote: false);
                    }
                    transportedPawn.Notify_Teleported();
                    CompAbilityEffect_Teleport.SendSkipUsedSignal(transportedPawn.Position, transportedPawn);
                }

                if (destClamorType != null)
                {
                    GenClamor.DoClamor(caster, destCell, destClamorRadius, destClamorType);
                }
            }

            if (cachedRT != null)
            {
                cachedRT.Release();
                cachedRT = null;
            }

            this.Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (age >= 30 || !this.Spawned) return;

            if (cachedThingMat == null && innerContainer.Count > 0)
                CacheTexture();

            float progress = age / 30f;
            int frame = Mathf.Clamp(age, 0, 29);

            Vector3 startPos = this.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead);
            Vector3 destPos = destCell.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead);
            startPos.y -= 0.1f;
            destPos.y -= 0.1f;

            bool isPawnOrCorpse = innerContainer.Count > 0 && (innerContainer[0] is Pawn || innerContainer[0] is Corpse);
            float startPortalOffsetZ = isPawnOrCorpse ? -0.8f : -0.3f;
            float destPortalOffsetZ = isPawnOrCorpse ? 1.2f : 0.7f;

            float portalHeight = 1f;
            float portalWidth = portalHeight * 255f / 64f;

            Material startPortalMat = MaterialPool.MatFrom(
                $"Other/gap/gap_invert/bulletCa{frame:D3}",
                ShaderDatabase.Mote);

            Vector3 startPortalPos = startPos;
            startPortalPos.z += startPortalOffsetZ;

            Matrix4x4 startMatrix = Matrix4x4.TRS(
                startPortalPos,
                Quaternion.identity,
                new Vector3(portalWidth, 1f, portalHeight));

            Graphics.DrawMesh(MeshPool.plane10, startMatrix, startPortalMat, 0);

            Material destPortalMat = MaterialPool.MatFrom(
                $"Other/gap/bulletCa{frame:D3}",
                ShaderDatabase.MoteGlow);

            Vector3 destPortalPos = destPos;
            destPortalPos.z += destPortalOffsetZ;

            Matrix4x4 destMatrix = Matrix4x4.TRS(
                destPortalPos,
                Quaternion.identity,
                new Vector3(portalWidth, 1f, portalHeight));

            Graphics.DrawMesh(MeshPool.plane10, destMatrix, destPortalMat, 0);

            if (cachedThingMat != null)
            {
                float W = thingDrawSize.x;
                float H = thingDrawSize.y;
                float bottomZ = -H / 2f;

                float sinkTopZ = H / 2f - progress * H;
                if (sinkTopZ > bottomZ)
                {
                    if (sinkMesh == null) sinkMesh = new Mesh();
                    UpdateMesh(sinkMesh, W, sinkTopZ, bottomZ, 1f, progress);

                    Vector3 thingStartPos = startPos;
                    thingStartPos.y += 0.05f;

                    Graphics.DrawMesh(sinkMesh, thingStartPos, Quaternion.identity, cachedThingMat, 0);
                }

                float fallBottomZ = H / 2f - progress * H;
                float fallTopZ = H / 2f;

                if (fallBottomZ < fallTopZ)
                {
                    if (riseMesh == null)
                        riseMesh = new Mesh();

                    UpdateMesh(
                        riseMesh,
                        W,
                        fallTopZ,
                        fallBottomZ,
                        progress,
                        0f);

                    Vector3 thingDestPos = destPos;
                    thingDestPos.y += 0.05f;

                    Graphics.DrawMesh(
                        riseMesh,
                        thingDestPos,
                        Quaternion.identity,
                        cachedThingMat,
                        0);
                }
            }
        }

        private void UpdateMesh(Mesh mesh, float width, float topZ, float bottomZ, float uvTop, float uvBottom)
        {
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-width / 2f, 0, bottomZ);
            vertices[1] = new Vector3(width / 2f, 0, bottomZ);
            vertices[2] = new Vector3(-width / 2f, 0, topZ);
            vertices[3] = new Vector3(width / 2f, 0, topZ);

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0, uvBottom);
            uvs[1] = new Vector2(1, uvBottom);
            uvs[2] = new Vector2(0, uvTop);
            uvs[3] = new Vector2(1, uvTop);

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
        }
    }
}