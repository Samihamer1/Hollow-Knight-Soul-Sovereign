using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections;
using Vasi;

namespace SoulSovereign.Attacks
{
    public class OrbSpawner : MonoBehaviour
    {
        private GameObject mageOrb;
        private GameObject[] Orbs = new GameObject[6];
        private GameObject whiteFlash;
        private GameObject fireEffect;
        private Sovereign sovereign;

        private bool Completed = false;

        public void setSovereign(Sovereign sovereign)
        {
            this.sovereign = sovereign;
        }

        private void Start()
        {
            ModifyOrbSpawner();
        }

        private void ModifyOrbSpawner()
        {
            GameObject orbSpawner = gameObject;

            PlayMakerFSM summonOrbs = orbSpawner.LocateMyFSM("Summon Orbs");
            if (summonOrbs != null)
            {
                mageOrb = Instantiate(summonOrbs.GetAction<SpawnObjectFromGlobalPool>("Spawn", 0).gameObject.Value);
                mageOrb.SetActive(false);
                ModifyMageOrb();

                whiteFlash = Instantiate(ResourceLoader.WhiteFlash);
                whiteFlash.SetActive(false);
                fireEffect = Instantiate(ResourceLoader.FireEffect);
                fireEffect.SetActive(false);

                FsmEvent ConvergeEvent = summonOrbs.CreateFsmEvent("CONVERGE");

                FsmState NewSpawnState = summonOrbs.CreateState("New Spawn");
                FsmState ConvergeState = summonOrbs.CreateState("Converge");
                FsmState IdleState = summonOrbs.GetState("Idle");

                NewSpawnState.AddMethod(CreateOrbs);

                IdleState.ChangeTransition("SPINNER SUMMON", "New Spawn");
                NewSpawnState.AddTransition(FsmEvent.Finished, "Idle");
            }

            PlayMakerFSM spinControl = orbSpawner.LocateMyFSM("Spin Control");
            if ( spinControl != null )
            {
                spinControl.FsmVariables.GetFsmFloat("Spin Speed").Value = 600f;
            }
        }

        private void CreateOrbs()
        {
            Completed = false;

            for (int i = 0; i < 6; i++)
            {
                GameObject newOrb = Instantiate(mageOrb);
                newOrb.transform.parent = gameObject.transform;
                Orbs[i] = newOrb;
                newOrb.SetActive(true);
            }

            SetOrbPosition(7);
        }

        private void SetOrbPosition(float radius)
        {
            float a = (float)(2 * Math.PI / 6);

            for (int i = 0; i < 6; i++)
            {
                GameObject newOrb = Orbs[i];

                Vector3 point = new Vector3((float)(radius * Math.Cos(a * i)), (float)(radius * Math.Sin(a * i)));
                if (newOrb == null) { continue; }
                newOrb.transform.localPosition = point;
            }
        }

        private void ModifyMageOrb()
        {
            GameObject orb = mageOrb;

            PlayMakerFSM orbControl = orb.LocateMyFSM("Orb Control");
            if (orbControl != null)
            {
                FsmEvent BurstEvent = orbControl.CreateFsmEvent("BURST");

                FsmState OrbitingState = orbControl.GetState("Orbiting");
                FsmState BurstInitState = orbControl.CreateState("Burst Init");
                FsmState BurstActiveState = orbControl.CreateState("Burst Active");

                FsmFloat Angle = orbControl.CreateFsmFloat("Burst Angle",0);

                FsmOwnerDefault OwnerDefault = orbControl.GetState("Chase Hero").GetAction<SetIsKinematic2d>().gameObject;

                BurstInitState.AddAction(new RandomFloat { min = 0, max = 360, storeResult = Angle });

                BurstActiveState.AddAction(new SetVelocityAsAngle { gameObject = OwnerDefault, speed = 10f, angle = Angle, everyFrame = false });

                OrbitingState.AddTransition(BurstEvent, "Burst Init");
                BurstInitState.AddTransition(FsmEvent.Finished, "Burst Active");              
            }

            mageOrb = orb;

        }

        private float calculateAngle(float x1, float y1, float x2, float y2)
        {
            // Calculate the difference in coordinates
            float dx = x2 - x1;
            float dy = y2 - y1;

            // Calculate the angle using atan2 function
            float angle = (float)Math.Atan2(dy, dx);

            return angle;
        }

        private void ShootOrbs()
        {          
            float xTotal = 0;
            float yTotal = 0;
            for (int i = 0; i < 6; i++)
            {
                GameObject orb = Orbs[i];
                orb.transform.parent = null;
                xTotal += orb.transform.GetPositionX();
                yTotal += orb.transform.GetPositionY();                
            }

            float averageX = xTotal / 6;
            float averageY = yTotal / 6;

            //fx
            whiteFlash.transform.position = new Vector3(averageX, averageY, 0.01f);
            whiteFlash.SetActive(true);

            fireEffect.transform.position = new Vector3(averageX, averageY, -0.03f);
            fireEffect.SetActive(true);

            sovereign.PlayShotSound();        

            for (int i = 0; i < 6; i++)
            {
                GameObject orb = Orbs[i];
                float angle = calculateAngle(averageX, averageY, orb.transform.GetPositionX(), orb.transform.GetPositionY());
                GameManager.instance.StartCoroutine(ShootSingleOrb(orb, angle));
            }

        }

        public void MoveOrb(GameObject orb, float distance, float angle)
        {
            float posX = orb.transform.GetPositionX();
            float posY = orb.transform.GetPositionY();

            posX += (float) (distance * Math.Cos(angle));
            posY += (float)(distance * Math.Sin(angle));

            orb.transform.SetPositionX(posX);
            orb.transform.SetPositionY(posY);
        }

        private IEnumerator ShootSingleOrb(GameObject orb, float angle)
        {
            ConstrainPosition constrain = orb.AddComponent<ConstrainPosition>();
            constrain.constrainX = true;
            constrain.constrainY = true;

            constrain.yMin = 28.4f;
            constrain.yMax = 41.1f;
            constrain.xMin = 3.4f;
            constrain.xMax = 36.1f;

            float deltaTime = 0;

            while (orb != null)
            {
                yield return new Wait();

                float distance = 18.5f;

                MoveOrb(orb, distance * Time.deltaTime, angle);

                float oldangle = angle;
                angle = Bounce(orb, angle);

                Vector2 newpos = orb.transform.position;

                //to prevent bounce spam
                if (oldangle != angle)
                {
                    newpos.x = Mathf.Clamp(newpos.x, 3.5f, 36f);
                    newpos.y = Mathf.Clamp(newpos.y, 28.5f, 41f);
                }

                deltaTime += Time.deltaTime;

                if (deltaTime > 3f)
                {
                    break;
                }
            }
            orb.LocateMyFSM("Orb Control").SendEvent("DISSIPATE");
        }

        private float Bounce(GameObject orb, float angle)
        {
            float topY = 41f;
            float bottomY = 28.5f;
            float leftX = 3.5f;
            float rightX = 36f;

            float orbX = orb.transform.GetPositionX();
            float orbY = orb.transform.GetPositionY();

            if (orbX <= leftX ||  orbX >= rightX)
            {
                angle = (float)(Math.PI - angle);
            }

            if (orbY  <= bottomY || orbY >= topY)
            {
                angle = -angle;
            }

            return angle;
        }

        private IEnumerator ConvergeCoroutine()
        {
            float radius = 7;
            while (!Completed)
            {
                yield return new Wait();

                if (radius < 0.1f)
                {
                    radius = 0.1f;
                }

                SetOrbPosition(radius);
                radius -= 10 * Time.deltaTime;
               
                if (radius <= 0.1f)
                {
                    ShootOrbs();
                    break;
                }
            }
        }

        public void Converge()
        {
            if (Orbs[0] == null) { return; }
            GameManager.instance.StartCoroutine(ConvergeCoroutine());
        }
    }
}

