using SoulSovereign.Attacks;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections;
using System.EnterpriseServices;
using UnityEngine;
using Vasi;

namespace SoulSovereign
{
    public class Sovereign2 : MonoBehaviour
    {
        private GameObject DivingWarrior;
        public AudioSource AudioSource;
        private Sovereign3 Sovereign3;

        public void SetSovereign3(Sovereign3 Sovereign3)
        {
            this.Sovereign3 = Sovereign3;
        }

        private void Start()
        {
            PlayMakerFSM SovereignControl = gameObject.LocateMyFSM("Mage Lord 2");
            if (SovereignControl != null)
            {
                ModifyFSM(SovereignControl);
            }

            gameObject.GetComponent<HealthManager>().hp = 800;

            ConstrainPosition constrainpos = gameObject.AddComponent<ConstrainPosition>();
            constrainpos.yMin = -20f;
            constrainpos.yMax = 999f;
            constrainpos.constrainY = true;

            //theres two corpses with the same name. this is the best i got
            GameObject corpse1 = gameObject.Child("Corpse Dream Mage Lord 2(Clone)");
            Destroy(corpse1.GetComponent<EndBossSceneTimer>());
            corpse1.name = "Corpse 1";
            corpse1.LocateMyFSM("corpse").GetState("End GG Scene").RemoveAction(0);
            corpse1.LocateMyFSM("corpse").GetState("End GG Scene").AddMethod(() => { Sovereign3.gameObject.SetActive(true); });
            //Destroy(corpse1.GetComponent<BoxCollider2D>());

            GameObject corpse2 = gameObject.Child("Corpse Dream Mage Lord 2(Clone)");
            Destroy(corpse2.GetComponent<EndBossSceneTimer>());
            corpse2.LocateMyFSM("corpse").GetState("End GG Scene").RemoveAction(0);
            corpse2.LocateMyFSM("corpse").GetState("End GG Scene").AddMethod(() => { Sovereign3.gameObject.SetActive(true); });
            //Destroy(corpse2.GetComponent<BoxCollider2D>());
        }

        private void ModifyFSM(PlayMakerFSM sovereignControl)
        {
            /*/ Modification Summary
             * Shockwaves re-added to Quake Land state.
             * Quake now has a 50% chance to be a horizontal dive.
             * Single-Use Soul Warrior (DivingWarrior) added to each of the orb shots
             * Phase 3 added to fight (See Sovereign3)
             */
            ObjectInit(sovereignControl);

            FsmGameObject warriorBuffer = sovereignControl.CreateFsmGameObject("Warrior Buffer");
            FsmGameObject heroObject = sovereignControl.CreateFsmGameObject("Hero Obj");
            FsmFloat heroX = sovereignControl.FsmVariables.GetFsmFloat("Hero X");
            FsmFloat heroY = sovereignControl.FsmVariables.GetFsmFloat("Hero Y");
            FsmEvent LeftEvent = sovereignControl.CreateFsmEvent("LEFT");
            FsmEvent RightEvent = sovereignControl.CreateFsmEvent("RIGHT");
            FsmEvent CancelEvent = sovereignControl.GetFsmEvent("CANCEL");
            FsmEvent AltEvent = sovereignControl.CreateFsmEvent("ALTERNATE");
            FsmFloat selfX = sovereignControl.FsmVariables.GetFsmFloat("Self X");
            FsmFloat rightX = sovereignControl.FsmVariables.GetFsmFloat("Right X");
            FsmFloat leftX = sovereignControl.FsmVariables.GetFsmFloat("Left X");
            FsmFloat teleX = sovereignControl.FsmVariables.GetFsmFloat("Tele X");
            FsmFloat teleY = sovereignControl.FsmVariables.GetFsmFloat("Tele Y");
            FsmFloat selfZ = sovereignControl.FsmVariables.GetFsmFloat("Self Z");
            FsmFloat selfY = sovereignControl.FsmVariables.GetFsmFloat("Self Y");
            FsmFloat quakeType = sovereignControl.CreateFsmFloat("Quake Type", 0);
            FsmVector3 lookatVector = sovereignControl.CreateFsmVector3("Look At Vector");

            FsmState SpawnFireballState = sovereignControl.GetState("Spawn Fireball");
            FsmState QuakeLandState = sovereignControl.GetState("Quake Land");
            FsmState QuakeAnticState = sovereignControl.GetState("Quake Antic");
            FsmState QuakeDownState = sovereignControl.GetState("Quake Down");
            FsmState TeleportQState = sovereignControl.GetState("TeleportQ");
            FsmState TeleLineQState = sovereignControl.GetState("Tele Line Q");
            FsmState ShiftState = sovereignControl.GetState("Shift?");
            FsmState ReactivateState = sovereignControl.GetState("Reactivate");
            FsmState TeleportSetLeftState = sovereignControl.CreateState("Teleport Set Left");
            FsmState TeleportSetRightState = sovereignControl.CreateState("Teleport Set Right");
            FsmState TeleportRushDirState = sovereignControl.CreateState("Teleport Rush Dir");
            FsmState TeleportRushState = sovereignControl.CreateState("Teleport Rush");
            FsmState TeleportRushLineState = sovereignControl.CreateState("Teleport Rush Line");
            FsmState TeleportRushLeftState = sovereignControl.CreateState("Teleport Rush Left");
            FsmState TeleportRushRightState = sovereignControl.CreateState("Teleport Rush Right");
            FsmState TeleportRushRightAnticState = sovereignControl.CreateState("Teleport Rush Right Antic");
            FsmState TeleportRushLeftAnticState = sovereignControl.CreateState("Teleport Rush Left Antic");
            FsmState TeleportRushLeftLandState = sovereignControl.CreateState("Teleport Rush Left Land");
            FsmState TeleportRushRightLandState = sovereignControl.CreateState("Teleport Rush Right Land");
            FsmState QuakeAnticChoiceState = sovereignControl.CreateState("Quake Antic Choice");
            FsmState RandomQuakeState = sovereignControl.CreateState("Random Quake");

            FsmOwnerDefault HeroOwnerDefault = sovereignControl.GetAction<GetPosition>("Fireball Pos", 4).gameObject;
            FsmOwnerDefault OwnerDefault = sovereignControl.GetAction<GetPosition>("Fireball Pos", 3).gameObject;

            SpawnFireballState.AddAction(new SpawnObjectFromGlobalPool { gameObject = DivingWarrior, spawnPoint = sovereignControl.gameObject, position = new Vector3(0, 0, 0), rotation = new Vector3(0, 0, 0), storeObject = warriorBuffer });
            SpawnFireballState.AddMethod(() => { warriorBuffer.Value.SetActive(true); });
            QuakeLandState.RemoveAction(13); //wait

            RandomQuakeState.AddAction(new SetFloatValue { floatVariable = quakeType, floatValue = 0, everyFrame = false });
            FsmEvent[] events = new FsmEvent[2] { AltEvent, CancelEvent };
            FsmFloat[] weights = new FsmFloat[2] { 0.5f, 0.5f };
            RandomQuakeState.AddAction(new SendRandomEvent { events = events, weights = weights, delay = 0f });

            TeleportRushDirState.AddAction(new GetPosition { gameObject = HeroOwnerDefault, vector = new Vector3(0, 0), x = heroX, y = heroY, z = 0, everyFrame = false, space = 0 });
            TeleportRushDirState.AddAction(new SetFloatValue { floatVariable = teleY, floatValue = heroY, everyFrame = false });
            TeleportRushDirState.AddAction(new FloatCompare { float1 = heroX, float2 = 20f, tolerance = 0f, everyFrame = false, equal = LeftEvent, greaterThan = RightEvent, lessThan = LeftEvent });

            TeleportSetLeftState.AddAction(new SetFloatValue { floatVariable = teleX, floatValue = 27, everyFrame = false });
            TeleportSetLeftState.AddAction(new SetFloatValue { floatVariable = quakeType, floatValue = 1, everyFrame = false });
            TeleportSetRightState.AddAction(new SetFloatValue { floatVariable = teleX, floatValue = 15, everyFrame = false });
            TeleportSetRightState.AddAction(new SetFloatValue { floatVariable = quakeType, floatValue = 2, everyFrame = false });

            //quake type 0-normal, 1-left, 2-right
            QuakeAnticChoiceState.AddAction(new SetDamageHeroAmount { damageDealt = 1, target = OwnerDefault });
            QuakeAnticChoiceState.AddAction(new SetInvincible { Invincible = false, InvincibleFromDirection = 0, target = OwnerDefault });
            QuakeAnticChoiceState.AddAction(new FloatCompare { float1 = quakeType, float2 = 0, tolerance = 0f, everyFrame = false, equal = CancelEvent });
            QuakeAnticChoiceState.AddAction(new FloatCompare { float1 = quakeType, float2 = 1, tolerance = 0f, everyFrame = false, equal = LeftEvent });
            QuakeAnticChoiceState.AddAction(new FloatCompare { float1 = quakeType, float2 = 2, tolerance = 0f, everyFrame = false, equal = RightEvent });

            TeleportRushLeftAnticState.CopyActionData(QuakeAnticState);
            TeleportRushLeftAnticState.GetAction<iTweenMoveBy>(1).vector = new Vector3(2, 0, 0);
            TeleportRushLeftAnticState.RemoveAction(2);
            TeleportRushLeftAnticState.AddAction(new GetHero { storeResult = heroObject });
            TeleportRushLeftAnticState.AddAction(new ChaseObjectVertical { gameObject = OwnerDefault, target = heroObject, speedMax = 5f, acceleration = 1f});
            TeleportRushLeftAnticState.AddMethod(() => { gameObject.transform.SetRotationZ(-90); });
           

            //spawn him at 27 for left rush
            //15 for right

            TeleportRushLeftState.CopyActionData(QuakeDownState);
            TeleportRushLeftState.GetAction<SetVelocity2d>(5).x = -75f;
            TeleportRushLeftState.GetAction<SetVelocity2d>(5).y = 0f;
            TeleportRushLeftState.GetAction<GetPosition>(6).x = selfX;
            TeleportRushLeftState.GetAction<FloatCompare>(7).float1 = selfX;
            TeleportRushLeftState.GetAction<FloatCompare>(7).float2 = leftX;

            TeleportRushLeftLandState.CopyActionData(QuakeLandState);
            TeleportRushLeftLandState.GetAction<SetPosition>(1).x = 5.3f;
            TeleportRushLeftLandState.GetAction<SetPosition>(1).y = selfY;
            TeleportRushLeftLandState.InsertAction(0,new GetPosition { gameObject = OwnerDefault, everyFrame = false, x = selfX, y = selfY, z = selfZ, space = 0, vector = new Vector3(0, 0) });

            //i dont like repeating code, but for some reason it doesnt copy the changes made to left antic and instead copies quake antic
            //figured i should just bite the bullet and make it copy quake antic on purpose
            //same deal for all of the right rush states

            TeleportRushRightAnticState.CopyActionData(QuakeAnticState);
            TeleportRushRightAnticState.GetAction<iTweenMoveBy>(1).vector = new Vector3(-2, 0, 0);
            TeleportRushRightAnticState.RemoveAction(2);
            TeleportRushRightAnticState.AddAction(new GetHero { storeResult = heroObject });
            TeleportRushRightAnticState.AddAction(new ChaseObjectVertical { gameObject = OwnerDefault, target = heroObject, speedMax = 5f, acceleration = 1f });
            TeleportRushRightAnticState.AddMethod(() => { gameObject.transform.SetRotationZ(90); });


            TeleportRushRightState.CopyActionData(QuakeDownState);
            TeleportRushRightState.GetAction<SetVelocity2d>(5).x = 75f;
            TeleportRushRightState.GetAction<SetVelocity2d>(5).y = 0f;
            TeleportRushRightState.GetAction<GetPosition>(6).x = selfX;
            TeleportRushRightState.GetAction<FloatCompare>(7).float1 = selfX;
            TeleportRushRightState.GetAction<FloatCompare>(7).float2 = rightX;
            TeleportRushRightState.GetAction<FloatCompare>(7).greaterThan = FsmEvent.Finished;
            TeleportRushRightState.GetAction<FloatCompare>(7).lessThan = new FsmEvent("");        
            TeleportRushRightState.AddAction(new FaceAngle { gameObject = OwnerDefault, everyFrame = true, angleOffset = 90f });

            TeleportRushRightLandState.CopyActionData(QuakeLandState);
            TeleportRushRightLandState.GetAction<SetPosition>(1).x = rightX;
            TeleportRushRightLandState.GetAction<SetPosition>(1).y = selfY;
            TeleportRushRightLandState.InsertAction(0, new GetPosition { gameObject = OwnerDefault, everyFrame = false, x = selfX, y = selfY, z = selfZ, space = 0, vector = new Vector3(0, 0) });

            ReactivateState.AddMethod(() => { gameObject.transform.SetRotationZ(0); });
            

            QuakeLandState.ChangeTransition("FINISHED", "Quake Waves");
            TeleportRushDirState.AddTransition(LeftEvent, "Teleport Set Left");
            TeleportRushDirState.AddTransition(RightEvent, "Teleport Set Right");
            TeleportRushLeftAnticState.AddTransition(FsmEvent.Finished, "Teleport Rush Left");
            TeleportRushLeftState.AddTransition(FsmEvent.Finished, "Teleport Rush Left Land");
            TeleportSetLeftState.AddTransition(FsmEvent.Finished, "TeleportQ");
            TeleportSetRightState.AddTransition(FsmEvent.Finished, "TeleportQ");
            TeleLineQState.ChangeTransition("FINISHED", "Quake Antic Choice");
            QuakeAnticChoiceState.AddTransition(CancelEvent, "Quake Antic");
            QuakeAnticChoiceState.AddTransition(LeftEvent, "Teleport Rush Left Antic");
            QuakeAnticChoiceState.AddTransition(RightEvent, "Teleport Rush Right Antic");
            ShiftState.ChangeTransition("FINISHED", "Random Quake");
            RandomQuakeState.AddTransition(AltEvent, "TeleportQ");
            RandomQuakeState.AddTransition(CancelEvent, "Teleport Rush Dir");
            TeleportRushLeftState.AddTransition(FsmEvent.Finished, "Teleport Rush Left Land");
            TeleportRushLeftLandState.AddTransition(FsmEvent.Finished, "Re Pos");
            TeleportRushRightAnticState.AddTransition(FsmEvent.Finished, "Teleport Rush Right");
            TeleportRushRightState.AddTransition(FsmEvent.Finished, "Teleport Rush Right Land");
            TeleportRushRightLandState.AddTransition(FsmEvent.Finished, "Re Pos");
        }

        private void ObjectInit(PlayMakerFSM sovereignControl)
        {
            InitDivingWarrior();
            AudioSource = gameObject.GetComponent<AudioSource>();
        }

        public void PlayShotSound()
        {
            AudioClip clip = (AudioClip)gameObject.LocateMyFSM("Mage Lord 2").GetAction<AudioPlaySimple>("Spawn Fireball", 6).oneShotClip.Value;
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
