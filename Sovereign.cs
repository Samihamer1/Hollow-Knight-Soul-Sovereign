using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using SoulSovereign.Attacks;
using System.Collections;
using System.EnterpriseServices;
using UnityEngine;
using Vasi;

namespace SoulSovereign
{
    public class Sovereign : MonoBehaviour
    {
        private GameObject DivingWarrior;
        private GameObject LargeZap;
        private OrbSpawner OrbSpawner;
        public AudioSource AudioSource;

        private void Start()
        {
            PlayMakerFSM SovereignControl = gameObject.LocateMyFSM("Mage Lord");
            if (SovereignControl != null)
            {
                ModifyFSM(SovereignControl);
            }
        }

        private void ModifyFSM(PlayMakerFSM sovereignControl)
        {
            /*/ Modification Summary
             * Second round of shockwaves created after the original Shockwaves state.
             * Single-Use Soul Warrior (DivingWarrior) added to each of the orb shots
             * Charge attack now tracks player.
             * Two more orbs added to the orb spinner, and the orbs converge then bounce around the screen. (See OrbSpawner)
             * Orb spinner attack now only travels halfway across screen.
             * New roar attack added that spawns 4 mage blobs, and 3 mage balloons.
             */
            ObjectInit(sovereignControl);

            FsmGameObject warriorBuffer = sovereignControl.CreateFsmGameObject("Warrior Buffer");
            FsmEvent CancelEvent = sovereignControl.GetFsmEvent("CANCEL");
            FsmEvent RoarEvent = sovereignControl.CreateFsmEvent("ROAR");
            FsmEvent HighSpinnerEvent = sovereignControl.GetFsmEvent("HIGH SPINNER");
            FsmEvent ChargeEvent = sovereignControl.GetFsmEvent("CHARGE");
            FsmEvent ShootEvent = sovereignControl.GetFsmEvent("SHOOT");
            FsmEvent QuakeEvent = sovereignControl.GetFsmEvent("QUAKE");
            FsmString NextEventString = sovereignControl.FsmVariables.GetFsmString("Next Event");
            FsmGameObject HeroObject = sovereignControl.CreateFsmGameObject("Hero Obj");
            FsmFloat Angle = sovereignControl.CreateFsmFloat("Angle",0);
            FsmFloat ChargeSpeed = sovereignControl.CreateFsmFloat("Charge Speed", 30f);
            FsmFloat RightX = sovereignControl.FsmVariables.GetFsmFloat("Right X");
            FsmFloat LeftX = sovereignControl.FsmVariables.GetFsmFloat("Left X");
            FsmFloat SelfX = sovereignControl.FsmVariables.GetFsmFloat("Self X");
            FsmFloat SelfY = sovereignControl.FsmVariables.GetFsmFloat("Self Y");
            FsmFloat HeroX = sovereignControl.FsmVariables.GetFsmFloat("Hero X");
            FsmFloat HeroY = sovereignControl.FsmVariables.GetFsmFloat("Hero Y");
            FsmFloat TeleX = sovereignControl.FsmVariables.GetFsmFloat("Tele X");
            FsmFloat TeleY = sovereignControl.FsmVariables.GetFsmFloat("Tele Y");
            FsmFloat TopY = sovereignControl.FsmVariables.GetFsmFloat("Top Y");
            FsmFloat GroundY = sovereignControl.FsmVariables.GetFsmFloat("Ground Y");
            FsmFloat PeakY = sovereignControl.CreateFsmFloat("Peak Y", 42f);
            FsmFloat ValleyY = sovereignControl.CreateFsmFloat("Valley Y", 26f);           
            FsmFloat TeleAdderXMax = sovereignControl.CreateFsmFloat("Tele Adder X Max", 6f);
            FsmFloat TeleAdderYMax = sovereignControl.CreateFsmFloat("Tele Adder Y Max", 3f);
            FsmFloat TeleAdderXMin = sovereignControl.CreateFsmFloat("Tele Adder X Min", -6f);
            FsmFloat TeleAdderYMin = sovereignControl.CreateFsmFloat("Tele Adder Y Min", -3f);
            FsmInt CtRoar = sovereignControl.GetOrCreateInt("Ct Roar");
            FsmInt MsRoar = sovereignControl.GetOrCreateInt("Ms Roar");
            FsmInt CtCharge = sovereignControl.GetOrCreateInt("Ct Charge");
            FsmInt MsCharge = sovereignControl.GetOrCreateInt("Ms Charge");
            FsmInt CtHS = sovereignControl.GetOrCreateInt("Ct HS");
            FsmInt MsHS = sovereignControl.GetOrCreateInt("Ms HS");
            FsmInt CtShoot = sovereignControl.GetOrCreateInt("Ct Shoot");
            FsmInt MsShoot = sovereignControl.GetOrCreateInt("Ms Shoot");
            FsmInt CtQuake = sovereignControl.GetOrCreateInt("Ct Quake");
            FsmInt MsQuake = sovereignControl.GetOrCreateInt("Ms Quake");
            FsmFloat RoarLeft = sovereignControl.CreateFsmFloat("Roar Left",0);
            FsmFloat RoarRight = sovereignControl.CreateFsmFloat("Roar Right", 0);
            FsmFloat RoarUp = sovereignControl.CreateFsmFloat("Roar Up", 0);
            FsmFloat RoarDown = sovereignControl.CreateFsmFloat("Roar Down", 0);


            sovereignControl.GetState("Shot").AddAction(new SpawnObjectFromGlobalPool { gameObject = DivingWarrior, spawnPoint = sovereignControl.gameObject, position = new Vector3(0, 0, 0), rotation = new Vector3(0, 0, 0), storeObject = warriorBuffer });
            sovereignControl.GetState("Shot").AddMethod(() => { warriorBuffer.Value.SetActive(true); });

            FsmState ChargeStopState = sovereignControl.GetState("Charge Stop");
            FsmState TeleChargeState = sovereignControl.GetState("Tele Charge");
            FsmState ChargeDirState = sovereignControl.GetState("Charge Dir");
            FsmState ChargeFireState = sovereignControl.CreateState("Charge Fire");
            FsmState TeleChargeRandomState = sovereignControl.CreateState("Tele Charge Random");
            FsmState TeleChargeRandomRedoState = sovereignControl.CreateState("Tele Charge Random Redo");
            FsmState TeleBotYState = sovereignControl.GetState("Tele Bot Y");
            FsmState HSTeleOutState = sovereignControl.GetState("HS Tele Out");
            FsmState HSOrbState = sovereignControl.GetState("HS Orb");
            FsmState HSLeftState = sovereignControl.GetState("HS Left");
            FsmState HSRightState = sovereignControl.GetState("HS Right");
            FsmState HSRetLeftState = sovereignControl.GetState("HS Ret Left");
            FsmState HSRetRightState = sovereignControl.GetState("Hs Ret Right"); //seriously?
            FsmState HSDissipateState = sovereignControl.GetState("HS Dissipate");
            FsmState QuakeDownState = sovereignControl.GetState("Quake Down");
            FsmState QuakeLandState = sovereignControl.GetState("Quake Land");
            FsmState RoarState = sovereignControl.GetState("Roar");
            FsmState RoarEndState = sovereignControl.GetState("Roar End");
            FsmState SummonRoarState = sovereignControl.CreateState("Summon Roar");
            FsmState SummonRoarEndState = sovereignControl.CreateState("Summon Roar End");
            FsmState SummonRoarSetState = sovereignControl.CreateState("Summon Roar Set");
            FsmState IdleState = sovereignControl.GetState("Idle");
            FsmState AttackChoiceState = sovereignControl.GetState("Attack Choice");
            FsmState AfterTeleState = sovereignControl.GetState("After Tele");
            FsmState QuakeWavesState = sovereignControl.GetState("Quake Waves");
            FsmState QuakeWavesReverseState = sovereignControl.CreateState("Quake Waves Reverse");
            FsmState ReposState = sovereignControl.GetState("Re Pos");

            FsmOwnerDefault OwnerDefault = ChargeDirState.GetAction<Tk2dPlayAnimation>(0).gameObject;
            FsmOwnerDefault HeroOwnerDefault = TeleChargeState.GetAction<GetPosition>(4).gameObject;

            ChargeDirState.AddAction(new GetHero { storeResult = HeroObject });
            ChargeDirState.AddAction(new GetAngleToTarget2D { everyFrame = false, gameObject = OwnerDefault, offsetX = 0f, offsetY = 0f, storeAngle = Angle, target = HeroObject });

            ChargeFireState.AddAction(new SetVelocityAsAngle { gameObject = OwnerDefault, angle = Angle, speed = ChargeSpeed, everyFrame = false });
            ChargeFireState.AddAction(new FaceObject { everyFrame = false, objectA = gameObject, objectB = HeroObject, spriteFacesRight = true, playNewAnimation = false, resetFrame = false, newAnimationClip = "" });
            ChargeFireState.AddAction(new GetPosition { gameObject = OwnerDefault, vector = new Vector3(0, 0), x = SelfX, y = SelfY, z = 0f, everyFrame = true, space = 0 });
            ChargeFireState.AddAction(new FloatCompare { float1 = SelfX, float2 = RightX, tolerance = 0f, greaterThan = FsmEvent.Finished, everyFrame = true });
            ChargeFireState.AddAction(new FloatCompare { float1 = SelfX, float2 = LeftX, tolerance = 0f, lessThan = FsmEvent.Finished, everyFrame = true });
            ChargeFireState.AddAction(new FloatCompare { float1 = SelfY, float2 = PeakY, tolerance = 0f, greaterThan = FsmEvent.Finished, everyFrame = true });
            ChargeFireState.AddAction(new FloatCompare { float1 = SelfY, float2 = ValleyY, tolerance = 0f, lessThan = FsmEvent.Finished, everyFrame = true });

            TeleChargeRandomState.AddAction(new GetPosition { gameObject = HeroOwnerDefault, vector = new Vector3(0, 0), x = HeroX, y = HeroY, space = 0, everyFrame = false, z = 0 });
            TeleChargeRandomState.AddAction(new GetPosition { gameObject = OwnerDefault, vector = new Vector3(0, 0), x = SelfX, y = SelfY, space = 0, everyFrame = false, z = 0 }); ;
            TeleChargeRandomState.AddAction(new RandomFloat { min = LeftX, max = RightX, storeResult = TeleX });
            TeleChargeRandomState.AddAction(new RandomFloat { min = GroundY, max = TopY, storeResult = TeleY });
            TeleChargeRandomState.AddAction(new FloatOperator { float1 = HeroX, float2 = 6f, operation = 0, storeResult = TeleAdderXMax, everyFrame = false });
            TeleChargeRandomState.AddAction(new FloatOperator { float1 = HeroX, float2 = -6f, operation = 0, storeResult = TeleAdderXMin, everyFrame = false });
            TeleChargeRandomState.AddAction(new FloatOperator { float1 = HeroY, float2 = 3f, operation = 0, storeResult = TeleAdderYMax, everyFrame = false });
            TeleChargeRandomState.AddAction(new FloatOperator { float1 = HeroY, float2 = -3f, operation = 0, storeResult = TeleAdderYMin, everyFrame = false });
            TeleChargeRandomState.AddAction(new FloatInRange { floatVariable = TeleX, lowerValue = TeleAdderXMin, upperValue = TeleAdderXMax, trueEvent = CancelEvent, everyFrame = false, boolVariable = false });
            TeleChargeRandomState.AddAction(new FloatInRange { floatVariable = TeleY, lowerValue = TeleAdderYMin, upperValue = TeleAdderYMax, trueEvent = CancelEvent, everyFrame = false, boolVariable = false });

            TeleChargeRandomRedoState.AddAction(new NextFrameEvent { sendEvent = FsmEvent.Finished });

            HSLeftState.GetAction<FloatCompare>(4).float2 = (LeftX.Value + RightX.Value) / 2;
            HSRightState.GetAction<FloatCompare>(2).float2 = (LeftX.Value + RightX.Value) / 2;

            HSRetLeftState.AddMethod(OrbSpawner.Converge);
            HSRetRightState.AddMethod(OrbSpawner.Converge);
            HSRetLeftState.RemoveAction<AccelerateVelocity>();
            HSRetRightState.RemoveAction<AccelerateVelocity>();

            HSDissipateState.RemoveAction<SendEventByName>();

            QuakeDownState.AddAction(new SetInvincible { Invincible = true, InvincibleFromDirection = 0, target = OwnerDefault });
            ReposState.AddAction(new SetInvincible { Invincible = false, InvincibleFromDirection = 0, target = OwnerDefault });

            SummonRoarState.CopyActionData(RoarState);
            SummonRoarState.RemoveAction(13);
            SummonRoarState.RemoveAction(12);
            SummonRoarState.RemoveAction(11);
            SummonRoarState.RemoveAction(10);
            SummonRoarState.RemoveAction(8);
            SummonRoarState.RemoveAction(7);
            SummonRoarState.AddMethod(SummonBlobs);
            AudioPlaySimple roarSound = sovereignControl.GetAction<AudioPlaySimple>("Teleport In", 1);
            SummonRoarState.AddAction(roarSound);
            SummonRoarState.AddAction(new SetInvincible { target = OwnerDefault, Invincible = true, InvincibleFromDirection = 0 });

            SummonRoarEndState.CopyActionData(RoarEndState);
            SummonRoarEndState.RemoveAction(1);
            SummonRoarEndState.AddAction(new SetInvincible { target = OwnerDefault, Invincible = false, InvincibleFromDirection = 0 });

            FsmEvent[] attackEvents = new FsmEvent[5] { ChargeEvent, HighSpinnerEvent, ShootEvent, QuakeEvent, RoarEvent };
            FsmFloat[] weightArray = new FsmFloat[5] { 0.15f, 0.15f, 0.25f, 0.3f, 0.15f };
            FsmInt[] trackingInts = new FsmInt[5] { CtCharge, CtHS, CtShoot, CtQuake, CtRoar };
            FsmInt[] eventMax = new FsmInt[5] { 1, 1, 2, 2, 1 };
            FsmInt[] trackingIntsMissed = new FsmInt[5] { MsCharge, MsHS, MsShoot, MsQuake, MsRoar };
            FsmInt[] missedMax = new FsmInt[5] { 6, 6, 4, 4, 6 };
            SendRandomEventV3 attackrandomevent = AttackChoiceState.GetAction<SendRandomEventV3>(1);
            attackrandomevent.events = attackEvents;
            attackrandomevent.weights = weightArray;
            attackrandomevent.trackingInts = trackingInts;
            attackrandomevent.eventMax = eventMax;
            attackrandomevent.trackingIntsMissed = trackingIntsMissed;
            attackrandomevent.missedMax = missedMax;

            SummonRoarSetState.AddAction(new SetStringValue { stringVariable = NextEventString, stringValue = "ROAR", everyFrame = false });
            SummonRoarSetState.AddAction(new SetFloatValue { floatVariable = TeleX, floatValue = (LeftX.Value + RightX.Value) / 2, everyFrame = false });
            SummonRoarSetState.AddAction(new SetFloatValue { floatVariable = TeleY, floatValue = (PeakY.Value + GroundY.Value) / 2, everyFrame = false });
            SummonRoarSetState.AddAction(new GetPosition { gameObject = HeroOwnerDefault, vector = new Vector3(0, 0), x = HeroX, y = HeroY, space = 0, everyFrame = false, z = 0 });                
            SummonRoarSetState.AddAction(new FloatOperator { float1 = TeleX, float2 = 4f, operation = 0, storeResult = RoarRight, everyFrame = false });
            SummonRoarSetState.AddAction(new FloatOperator { float1 = TeleX, float2 = -4f, operation = 0, storeResult = RoarLeft, everyFrame = false });
            SummonRoarSetState.AddAction(new FloatOperator { float1 = TeleY, float2 = 2f, operation = 0, storeResult = RoarUp, everyFrame = false });
            SummonRoarSetState.AddAction(new FloatOperator { float1 = TeleY, float2 = -2f, operation = 0, storeResult = RoarDown, everyFrame = false });
            SummonRoarSetState.AddAction(new FloatInRange { floatVariable = HeroX, lowerValue = RoarLeft, upperValue = RoarRight, trueEvent = CancelEvent, everyFrame = false, boolVariable = false });
            SummonRoarSetState.AddAction(new FloatInRange { floatVariable = HeroY, lowerValue = RoarDown, upperValue = RoarUp, trueEvent = CancelEvent, everyFrame = false, boolVariable = false });

            QuakeWavesReverseState.CopyActionData(QuakeWavesState);
            QuakeWavesReverseState.GetAction<SetPosition>(1).x = 4.4f;
            QuakeWavesReverseState.GetAction<SetPosition>(6).x = 36f;

            AfterTeleState.InsertAction(0, new SetMeshRenderer { gameObject = OwnerDefault, active = true }); //just in case
            AfterTeleState.InsertAction(0, new SetInvincible { target = OwnerDefault, Invincible = false, InvincibleFromDirection = 0 });

            ChargeDirState.RemoveTransition("Charge Left");
            ChargeDirState.RemoveTransition("Charge Right");
            ChargeDirState.AddTransition(FsmEvent.Finished, "Charge Fire");
            ChargeFireState.AddTransition(FsmEvent.Finished, "Charge Stop");
            TeleBotYState.ChangeTransition("FINISHED", "Tele Charge Random");
            TeleChargeRandomState.AddTransition(FsmEvent.Finished, "Teleport");
            TeleChargeRandomState.AddTransition(CancelEvent, "Tele Charge Random Redo");
            TeleChargeRandomRedoState.AddTransition(FsmEvent.Finished, "Tele Charge Random");
            IdleState.AddTransition(RoarEvent, "Summon Roar Set");
            SummonRoarState.AddTransition(FsmEvent.Finished, "Summon Roar End");
            SummonRoarEndState.AddTransition(FsmEvent.Finished, "Set Idle Timer");
            AttackChoiceState.AddTransition(RoarEvent, "Summon Roar Set");
            SummonRoarSetState.AddTransition(FsmEvent.Finished, "Teleport");
            SummonRoarSetState.AddTransition(CancelEvent, "Attack Choice");
            AfterTeleState.AddTransition("ROAR", "Summon Roar");
            QuakeWavesState.ChangeTransition("FINISHED", "Quake Waves Reverse");
            QuakeWavesReverseState.AddTransition(FsmEvent.Finished, "Re Pos");
        }

        private void SummonBlobs()
        {
            int left = 8;
            int right = 35;
            float difference = (right - left) / 4;           
            Vector3 heropos = HeroController.instance.transform.position;

            float herox = heropos.x;
            float heroy = heropos.y;
            System.Random random = new System.Random();
            for (int i = 0; i < 4; i++)
            {
                float spawny = 28.9096f;
                float spawnx = random.Next(left*10, right*10) / 10;

                //mage blob one
                while (CheckCollision(spawnx, spawny, herox, heroy, 3))
                {
                    spawnx = random.Next(left * 10, right * 10) / 10;
                }

                GameObject blob = Instantiate(ResourceLoader.MageBlob1);
                blob.SetActive(true);
                blob.transform.position = new Vector3(spawnx, spawny, blob.transform.GetPositionZ());

                PlayMakerFSM blobfsm = blob.LocateMyFSM("Blob");
                blobfsm.SendEvent("SPAWN");
            }

            for (int i = 0; i < 3; i++)
            {
                float spawny = random.Next(31 * 10, 41 * 10) / 10;
                float spawnx = random.Next(left * 10, right * 10) / 10;

                //mage blob one
                while (CheckCollision(spawnx, spawny, herox, heroy, 3))
                {
                    spawnx = random.Next(left * 10, right * 10) / 10;
                    spawny = random.Next(31 * 10, 41 * 10) / 10;
                }

                GameObject balloon = Instantiate(ResourceLoader.MageBalloon);
                balloon.SetActive(false);
                balloon.transform.position = new Vector3(spawnx, spawny, balloon.transform.GetPositionZ());
                GameManager.instance.StartCoroutine(DelayedActivate(balloon));
            }
        }

        private IEnumerator DelayedActivate(GameObject balloon)
        {
            yield return new Wait();
            balloon.SetActive(true);
        }

        private bool CheckCollision(float x1, float y1, float x2, float y2, float range)
        {
            if (x1 + range > x2 && x1 - range < x2 && y1 + range > y2 && y1 - range < y2)
            {
                //out of range, you're fine
                return true;
            }
            return false;
        }

        private void ObjectInit(PlayMakerFSM sovereignControl)
        {
            InitDivingWarrior();
            ModifyOrbSpawner(sovereignControl);          
            AudioSource = gameObject.GetComponent<AudioSource>();
        }

        private void ModifyOrbSpawner(PlayMakerFSM sovereignControl)
        {
            GameObject orbSpinner = sovereignControl.GetAction<SendEventByName>("HS Orb", 0).eventTarget.gameObject.GameObject.Value;
            OrbSpawner = orbSpinner.AddComponent<OrbSpawner>();
            OrbSpawner.setSovereign(this);
        }

        public void PlayShotSound()
        {
            AudioClip clip = (AudioClip)gameObject.LocateMyFSM("Mage Lord").GetAction<AudioPlaySimple>("Shot", 1).oneShotClip.Value;
            gameObject.GetComponent<AudioSource>().PlayOneShot(clip);
        }

        private void InitDivingWarrior()
        {
            GameObject origWarrior = ResourceLoader.MageKnight;
            GameObject divingWarrior = Instantiate(origWarrior);
            divingWarrior.AddComponent<DivingWarrior>();
            DivingWarrior = divingWarrior;
        }
    }
}
