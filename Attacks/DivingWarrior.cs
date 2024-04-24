using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Vasi;

namespace SoulSovereign.Attacks
{
    public  class DivingWarrior : MonoBehaviour
    {
        private void Start()
        {
            InitDivingWarrior();
        }

        private void InitDivingWarrior()
        {
            GameObject divingWarrior = gameObject;
            Destroy(divingWarrior.GetComponent<HealthManager>());

            GameObject quakeBlast = Instantiate(ResourceLoader.QuakeBlast);
            quakeBlast.SetActive(false);
            quakeBlast.transform.parent = gameObject.transform;
            quakeBlast.transform.SetScaleMatching(1.85f);
            quakeBlast.transform.localPosition = new Vector3(0.47f, -0.55f);

            GameObject quakePillar = Instantiate(ResourceLoader.QuakePillar);
            quakePillar.SetActive(false);
            quakePillar.transform.parent = gameObject.transform;
            quakePillar.transform.localPosition = new Vector3(-0.18f, 7.04f);

            GameObject whiteFlash = Instantiate(ResourceLoader.WhiteFlash);
            whiteFlash.SetActive(false);
            whiteFlash.transform.parent = gameObject.transform;
            whiteFlash.transform.localPosition = new Vector3(-0.14f, -0.14f, 0.01f);

            GameObject appearFlash = Instantiate(ResourceLoader.AppearFlash);
            appearFlash.SetActive(false);
            appearFlash.transform.parent = gameObject.transform;
            appearFlash.transform.localPosition = new Vector3(0, 0, -0.5f);


            PlayMakerFSM warriorControl = divingWarrior.LocateMyFSM("Mage Knight");
            if (warriorControl != null)
            { 

                FsmEvent FinishEvent = warriorControl.GetFsmEvent("FINISHED");

                //states
                FsmState StompRecoverState = warriorControl.GetState("Stomp Recover");
                FsmState TeleAnticState = warriorControl.GetState("Tele Antic");
                FsmState InitState = warriorControl.GetState("Init");
                FsmState WakeState = warriorControl.GetState("Wake");
                FsmState SlashRecoverState = warriorControl.GetState("Slash Recover");
                FsmState SideTeleState = warriorControl.GetState("Side Tele");
                FsmState StompSlashState = warriorControl.GetState("Stomp Slash");
                FsmState ShootState = warriorControl.GetState("Shoot");
                FsmState UpTeleAimState = warriorControl.GetState("Up Tele Aim");

                SendEventByName shakeEvent = ShootState.GetAction<SendEventByName>(2);
                shakeEvent.sendEvent = "AverageShake";

                Tk2dPlayAnimationWithEvents teleportanticEvent = TeleAnticState.GetAction<Tk2dPlayAnimationWithEvents>();
                SetVelocity2d telportvelocityEvent = TeleAnticState.GetAction<SetVelocity2d>();

                FsmState TeleOutState = warriorControl.CreateState("Tele Out");
                TeleOutState.AddMethod(() => { Destroy(divingWarrior); });

                FsmState TeleportAnticFlashlessState = warriorControl.CreateState("Tele Antic Flashless");
                TeleportAnticFlashlessState.AddAction(teleportanticEvent);
                TeleportAnticFlashlessState.AddAction(telportvelocityEvent);

                WakeState.AddMethod(() => { divingWarrior.GetComponent<SpriteFlash>().FlashingWhiteStay(); });
                StompSlashState.AddMethod(() => { quakeBlast.SetActive(true); quakePillar.SetActive(true); whiteFlash.SetActive(true);
                    divingWarrior.GetComponent<MeshRenderer>().enabled = false;
                    divingWarrior.GetComponent<BoxCollider2D>().enabled = false;
                });
                StompSlashState.AddAction(shakeEvent);
                TeleAnticState.AddMethod(() => { appearFlash.SetActive(true); whiteFlash.SetActive(true); });

                SideTeleState.GetAction<SetPosition>(6).y = 31.5f;

                //transitions
                StompRecoverState.ChangeTransition("FINISHED", "Tele Antic Flashless");
                SlashRecoverState.ChangeTransition("FINISHED", "Tele Antic");
                TeleAnticState.ChangeTransition("FINISHED", "Tele Out");
                InitState.ChangeTransition("FINISHED", "Wake");
                WakeState.ChangeTransition("FINISHED", "Up Tele Aim");
                SideTeleState.ChangeTransition("FINISHED", "Slash Aim");
                TeleportAnticFlashlessState.AddTransition(FinishEvent, "Tele Out");
            }
        }
    }
}

