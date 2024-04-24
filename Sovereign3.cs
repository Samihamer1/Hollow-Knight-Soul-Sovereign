using HutongGames.PlayMaker.Actions;
using IL;
using System.Collections;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UIElements;
using Vasi;
using Random = System.Random;

namespace SoulSovereign
{
    public class Sovereign3 : MonoBehaviour
    {
        private HealthManager healthManager;
        private SpriteFlash spriteFlash;
        private tk2dSpriteAnimator animator;
        private tk2dSprite sprite;
        private EnemyDreamnailReaction dreamnailReaction;
        private GameObject appearFlash;
        private GameObject hero;
        private MeshRenderer renderer;
        private Dictionary<Func<IEnumerator>, int> ctAttacks;
        private Dictionary<Func<IEnumerator>, int> msAttacks;

        private GameObject soulOrb;
        private AudioClip orbImpactClip;
        private GameObject soulLaser;
        private GameObject teleLine;
        private List<GameObject> lasers;

        private int previousLaserChoice = -1;
        private int moveCount = 0;

        private Random rand = new Random();
        private bool isDead = false;
        private void Start()
        {
            PlayMakerFSM SovereignControl = gameObject.LocateMyFSM("Mage Lord 2");
            if (SovereignControl != null)
            {
                SovereignControl.enabled = false; //i'll be handling this phase through code, not fsms
            }

            ObjectInit(SovereignControl);

            healthManager = gameObject.GetComponent<HealthManager>();
            healthManager.hp = 5;
            healthManager.IsInvincible = true;

            spriteFlash = gameObject.GetComponent<SpriteFlash>();
            spriteFlash.flash(new Color(1, 1, 1), 1, 0, 99999999, 0);

            animator = gameObject.GetComponent<tk2dSpriteAnimator>();

            sprite = gameObject.GetComponent<tk2dSprite>();

            renderer = gameObject.GetComponent<MeshRenderer>();

            dreamnailReaction = gameObject.GetComponent<EnemyDreamnailReaction>();

            foreach (InfectedEnemyEffects effects in gameObject.GetComponents<InfectedEnemyEffects>())
            {
                Destroy(effects);
            }

            foreach (EnemyDeathEffects effects in gameObject.GetComponents<EnemyDeathEffects>())
            {
                Destroy(effects);
            }

            foreach (ExtraDamageable effects in gameObject.GetComponents<ExtraDamageable>())
            {
                Destroy(effects);
            }

            hero = HeroController.instance.gameObject;

            lasers = new List<GameObject>();

            ctAttacks = new Dictionary<Func<IEnumerator>, int>
            {
                [OrbAttack] = 0,
                [DoomCircleAttack] = 0,
                [DoomerCircleAttack] = 0,
                [SpiritBombAttack] = 0,
                [TrackingOrbAttack] = 0
            };

            msAttacks = new Dictionary<Func<IEnumerator>, int>
            {
                [OrbAttack] = 3,
                [DoomCircleAttack] = 2,
                [DoomerCircleAttack] = 2,
                [SpiritBombAttack] = 1,
                [TrackingOrbAttack] = 1
            };

            StartCoroutine(Attacks());
            renderer.enabled = true;

            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
        }

        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            orig(self);
            if (self == dreamnailReaction)
            {
                spriteFlash.flash(new Color(1, 1, 1), 1, 0, 99999999, 0);
            }
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            //maybe the single weirdest thing ive ever seen in hollow knight
            //as far as my understand goes, the takedamage function directly calls the hit effect
            //i do not want the hit effects. if i disable them, they are called anyway.
            //if i delete them, they cause a nullreference error
            //so i took it into my own hands
            if (self == healthManager)
            {
                if (self.hp <= 0)
                {
                    return; //seperated so that there is still a hit effect on the killing blow, but you cant kill twice
                }
                self.hp -= 1; //not damage based. number of hit based.
                if (self.hp <= 0)
                {
                    isDead = true;
                }
                spriteFlash.flash(new Color(1, 1, 1), 1, 0, 99999999, 0);
                GameObject stunfx = Instantiate(ResourceLoader.StunFX);
                stunfx.transform.position = gameObject.transform.position;
            } else
            {
                orig(self, hitInstance);
            }
        }

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
        }

        private IEnumerator Attacks()
        {
            /*/ Phase Summary
             * Every 4 attacks, Vulnerable is called to the player to attack. After taking 25 damage (you can do more than this if the last hit brings you over 25) the boss teleports away, and 4 more attacks occur.
             * Beam Swap occurs immediately after Vulnerable
             * Beam Swap cycles through 4 orientations of lasers on the screen as obstacles
             * OrbAttack - a barrage of orbs aimed at the player from offscreen
             * DoomCircleAttack - A volley of randomly spawning circles of orbs that disperse outwards
             * DoomerCircleAttack - The same circle of orbs, but at one location and spammed like hell.
             * SpiritBombAttack - An orb spawns and increases in size with satellite orbs spawning randomly to converge into the large one. The large one is shot at the player then bounces around the screen. Repeats two more times.
             * TrackingOrbAttack - Series of orbs that spawn, hang, then shoot at player. 1, then 2, then 4, then 8, then 16.
             */
            yield return new WaitForSeconds(5f);

            yield return new WaitForEndOfFrame();

            while (!isDead)
            {                
                Vector2 HeroPos = hero.transform.position;
                Vector2 SelfPos = gameObject.transform.position;

                List<Func<IEnumerator>> currentAttacks;
                currentAttacks = new List<Func<IEnumerator>>{
                        OrbAttack, DoomCircleAttack, DoomerCircleAttack, SpiritBombAttack, TrackingOrbAttack
                };

                Func<IEnumerator> activeAttack = ChooseAttack(currentAttacks, msAttacks, ctAttacks);

                yield return activeAttack();

                moveCount++;

                if (moveCount % 4 == 0)
                {
                    yield return Vulnerable();
                    if (!isDead)
                    {
                        yield return BeamSwap();
                    }
                }

                yield return new WaitForEndOfFrame();
            }

            yield return Die();
        }

        private IEnumerator Die()
        {           
            yield return new Wait();

            float scale = 1f;

            if (HeroController.instance.transform.position.x < gameObject.transform.position.x)
            {
                scale = -1f;
            }

            gameObject.transform.SetScaleX(scale);

            GameObject roar = Instantiate(ResourceLoader.RoarFX);
            roar.transform.position = gameObject.transform.position;

            GameObject whiteWave = Instantiate(ResourceLoader.WhiteWave);
            whiteWave.transform.position = gameObject.transform.position;
            whiteWave.SetActive(true);

            GameObject deathParticles = Instantiate(ResourceLoader.DeathParticles);
            deathParticles.SetActive(true);
            deathParticles.transform.position = gameObject.transform.position;
            ParticleSystem particles = deathParticles.GetComponent<ParticleSystem>();
            particles.Play();

            gameObject.GetComponent<AudioSource>().PlayOneShot(ResourceLoader.RoarAudio);

            animator.Play("Roar");
            BigCameraShake();
            gameObject.GetComponent<BoxCollider2D>().enabled = false;

            float counter = 0;
            while (counter < 3)
            {
                counter += Time.deltaTime;

                float opacity = 1 - (counter / 3);

                sprite.color = new Color(1, 1, 1, opacity);

                yield return new WaitForEndOfFrame();
            }

            Destroy(roar);

            particles.Stop();

            GameCameras.instance.StopCameraShake();

            yield return new WaitForSeconds(3f);

            BossSceneController.Instance.EndBossScene();
        }

        private Func<IEnumerator> ChooseAttack(List<Func<IEnumerator>> currentAttacks, Dictionary<Func<IEnumerator>,int> msAttacks, Dictionary<Func<IEnumerator>,int> ctAttacks)
        {
            Func<IEnumerator> picked = currentAttacks[0];
            while (true)
            {
                int choice = rand.Next(0, currentAttacks.Count);
                picked = currentAttacks[choice];
                int missedMax = msAttacks[picked];
                int count = ctAttacks[picked];

                if (count < missedMax)
                {
                    for (int i = 0; i < ctAttacks.Count; i++)
                    {
                        Func<IEnumerator> func = ctAttacks.ToArray()[i].Key;
                        ctAttacks[func] = 0;
                    }

                    ctAttacks[picked] = count + 1;
                    break;
                }
            }

            return picked;
        }

        private IEnumerator Vulnerable()
        {
            yield return DestroyLasers();

            renderer.enabled = true;

            Vector2 heroPos = HeroController.instance.transform.position;

            float teleX = 15f;

            if (heroPos.x <= 20)
            {
                teleX = 27;
            }

            gameObject.transform.SetPosition2D(new Vector2(teleX, 31f));

            appearFlash.SetActive(true);

            yield return animator.PlayAnimWait("Tele In");

            animator.Play("Idle");

            healthManager.IsInvincible = false;


            //the most bootleg way of detecting a hit
            float count = 0;
            int hp = healthManager.hp;
            while (count < 8)
            {
                count += Time.deltaTime;

                if (healthManager.hp < (hp))
                {
                    break;
                }

                yield return new WaitForEndOfFrame();
            }

            healthManager.IsInvincible = true;

            if (!isDead) { 
                yield return animator.PlayAnimWait("Tele Out");
                renderer.enabled = false;

                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator SpiritBombAttack()
        {
            StartCoroutine(SpiritBomb(15f));
            yield return new WaitForSeconds(5f);
            StartCoroutine(SpiritBomb(10f));
            yield return new WaitForSeconds(5f);
            yield return SpiritBomb(5f);
            yield return new WaitForSeconds(2f);
        }

        private IEnumerator SpiritBomb(float lifetime)
        {
            Vector2 pos = GetRandomPos();

            GameObject appearflash = Instantiate(appearFlash);
            appearflash.transform.position = pos;
            appearflash.SetActive(true);

            yield return new WaitForSeconds(0.5f);

            StartCoroutine(CreateBigOrb(10, pos, lifetime));

            for (int i = 0; i < 10; i++)
            {
                yield return SpawnOrb(pos, 30f, 1f, 1f);
            }


            yield return new WaitForSeconds(5f);
        }

        private IEnumerator CreateBigOrb(int repeats, Vector2 pos, float lifetime)
        {
            GameObject orb = Instantiate(soulOrb);
            orb.SetActive(true);
            orb.transform.position = pos;

            //flash doesnt work on the same frame for some reason
            yield return new Wait();
            SpriteFlash flash = orb.GetComponent<SpriteFlash>();
            flash.FlashingWhiteStay();
            
            float sizeIncrease = 4f / repeats;
            float chargeTime = repeats * 0.5f;

            float counter = 0;

            while (counter < chargeTime)
            {
                counter += Time.deltaTime;

                float scale = orb.transform.GetScaleX();

                orb.transform.SetScaleMatching(scale + (sizeIncrease * Time.deltaTime));

                yield return new Wait();
            }

            Vector2 heroPos = HeroController.instance.transform.position;
            float angle = GetAngleFromStartToEnd(pos, heroPos);

            StartCoroutine(BounceVelocity(orb, angle, lifetime, 15f));
        }

        private IEnumerator TrackingOrbAttack()
        {
            int batch = 1;
            for (int i = 0; i < 5; i++)
            {
                yield return TrackingOrbBatch(batch);
                yield return new WaitForSeconds(2.5f + (0.25f * batch));
                batch *= 2;
            }
            yield return new WaitForSeconds(4f);
        }

        private IEnumerator TrackingOrbBatch(int number)
        {
            for (int i = number; i > 0; i--)
            {
                float extraDelay = i * 0.5f; 
                StartCoroutine(CreateTrackingOrb(extraDelay));
                yield return new WaitForSeconds(0.25f);
            }
        }

        private IEnumerator CreateTrackingOrb(float extraDelay)
        {
            Vector2 pos = GetRandomPos();
           
            while (Vector2.Distance(pos, HeroController.instance.transform.position) <= 5f)
            {
                pos = GetRandomPos();
            }

            GameObject appearflash = Instantiate(appearFlash);
            appearflash.transform.position = pos;
            appearflash.transform.SetScaleMatching(0.5f);
            appearflash.SetActive(true);

            yield return new WaitForSeconds(0.5f);

            //this segment of code is repeated in 3 functions because i couldn't figure out how to make it reusable
            //idk if ienumerators can return gameobjects
            GameObject orb = Instantiate(soulOrb);
            orb.SetActive(true);

            //flash doesnt work on the same frame for some reason
            yield return new Wait();
            SpriteFlash flash = orb.GetComponent<SpriteFlash>();
            flash.FlashingWhiteStay();

            orb.transform.position = pos;

            yield return new WaitForSeconds(1.5f);

            yield return new WaitForSeconds(extraDelay);

            float angle = GetAngleFromStartToEnd(orb.transform.position, HeroController.instance.transform.position);

            StartCoroutine(Velocity(orb, angle, 3f, 16f, 0f));
        }

        private IEnumerator DoomCircleAttack()
        {
            int random = rand.Next(6, 8);
            for (int i = 0; i < random; i++)
            {
                float randX = rand.Next(40, 360) / 10f;
                float randY = rand.Next(300, 400) / 10f;

                StartCoroutine(CreateDoomCircle(true, new Vector2(randX, randY), 1f));
                yield return new WaitForSeconds(0.85f);
            }
        }

        private Vector2 GetRandomPos()
        {
            float randX = rand.Next(40, 360) / 10f;
            float randY = rand.Next(300, 400) / 10f;

            return new Vector2(randX, randY);
        }

        private IEnumerator DoomerCircleAttack()
        {
            int random = rand.Next(18, 22);

            Vector2 pos = GetRandomPos();

            StartCoroutine(CreateDoomCircle(true, pos, 0.75f));

            yield return new WaitForSeconds(0.75f);

            for (int i = 0; i < random; i++)
            {
                StartCoroutine(CreateDoomCircle(false, pos, 0.75f));
                float delay = 0.7f - (i * 0.05f);
                if (delay < 0.05f)
                {
                    delay = 0.05f;
                }
                yield return new WaitForSeconds(delay);
            }

            yield return new WaitForSeconds(1.5f);
        }

        private IEnumerator CreateDoomCircle(bool flashAppear, Vector2 position, float speedmult)
        {
            GameObject[] Orbs = new GameObject[12];
            GameObject Base = new GameObject();
            Base.transform.SetRotation2D(rand.Next(0, 361));

            GameObject appearflash = Instantiate(appearFlash);
            appearflash.transform.parent = Base.transform;
            appearflash.SetActive(flashAppear);

            Base.transform.position = position;

            yield return new WaitForSeconds(0.5f);

            for (int i = 0; i < 12; i++)
            {
                GameObject orb = Instantiate(soulOrb);
                orb.SetActive(true);
                orb.transform.parent = Base.transform;

                //flash doesnt work on the same frame for some reason
                yield return new Wait();
                SpriteFlash flash = orb.GetComponent<SpriteFlash>();
                flash.FlashingWhiteStay();

                Orbs[i] = orb;
            }

            //set orb radius continuously
            float radius = 0;
            while (radius < 100)
            {
                yield return new WaitForEndOfFrame();
                radius += 20 * Time.deltaTime * speedmult;
                SetCircleRadius(Orbs, radius);
            }

            Destroy(Base);
        }

        private void SetCircleRadius(GameObject[] Orbs, float radius)
        {
            float a = (float)(2 * Math.PI / Orbs.Length);

            for (int i = 0; i < 12; i++)
            {
                GameObject newOrb = Orbs[i];
                
                Vector3 point = new Vector3((float)(radius * Math.Cos(a * i)), (float)(radius * Math.Sin(a * i)));
                if (newOrb == null) { continue; }
                newOrb.transform.localPosition = point;
            }
        }

        private void CreateLaser(Vector2 pos, float rotation)
        {
            GameObject newlaser = Instantiate(ResourceLoader.RadianceLaser);
            newlaser.SetActive(true);
            newlaser.transform.SetRotation2D(rotation);
            newlaser.transform.SetPosition2D(pos.x, pos.y);

            lasers.Add(newlaser);
        }

        private IEnumerator DestroyLasers()
        {
            foreach (GameObject laser in lasers)
            {
                PlayMakerFSM fsm = laser.GetComponent<PlayMakerFSM>();
                fsm.SendEvent("END");
            }

            yield return new WaitForSeconds(0.5f);

            foreach (GameObject laser in lasers)
            {
                Destroy(laser);
            }

            lasers.Clear();
        }

        private IEnumerator BeamSwap()
        {
            yield return DestroyLasers();
            previousLaserChoice += 1;
            if (previousLaserChoice > 3)
            {
                previousLaserChoice = 0;
            }

            switch (previousLaserChoice)
            {
                case 0:
                    //split into thirds
                    CreateLaser(new Vector2(15.3f, 19f), 90);
                    CreateLaser(new Vector2(26.6f, 19f), 90);
                    break;
                case 1:
                    //block half horizontally
                    CreateLaser(new Vector2(-1f, 35f), 0);
                    break;
                case 2:
                    //block walls
                    CreateLaser(new Vector2(3.18f, 19f), 90);
                    CreateLaser(new Vector2(36.7f, 19f), 90);
                    break;
                case 3:                    
                    //block floor
                    CreateLaser(new Vector2(-1f, 28.4f), 0);
                    break;
            }

            foreach (GameObject laser in lasers)
            {
                PlayMakerFSM fsm = laser.GetComponent<PlayMakerFSM>();
                fsm.SendEvent("ANTIC");
            }

            ResourceLoader.LaserPrepareAudio.LoadAudioData();
            gameObject.GetComponent<AudioSource>().PlayOneShot(ResourceLoader.LaserPrepareAudio, 2f);

            yield return new WaitForSeconds(2f);

            foreach (GameObject laser in lasers)
            {
                PlayMakerFSM fsm = laser.GetComponent<PlayMakerFSM>();
                fsm.SendEvent("FIRE");
            }
            ResourceLoader.LaserFireAudio.LoadAudioData();
            gameObject.GetComponent<AudioSource>().PlayOneShot(ResourceLoader.LaserFireAudio, 2f);

            yield return new WaitForSeconds(1.5f);
        }

        private IEnumerator OrbAttack()
        {
            int number = rand.Next(12, 19);
            for (int i = 0; i < number; i++)
            {
                Vector2 pos = HeroController.instance.transform.position;
                yield return SpawnOrb(pos, 30f, 2f, 2f);                
            }
            yield return new WaitForSeconds(1.5f);
        }

        private IEnumerator SpawnOrb(Vector2 position, float distance, float lineMultiplier, float travelMultiplier)
        {            
            //line multiplier is how much further the teleport line will travel than the orb, 1f is equal
            //travel multiplier is how much further the orb will travel past the origin from the distance it is placed at, 1f equal
            GameObject orb = Instantiate(soulOrb);
            orb.SetActive(true);            

            //flash doesnt work on the same frame for some reason
            yield return new Wait();
            SpriteFlash flash = orb.GetComponent<SpriteFlash>();
            flash.FlashingWhiteStay();

            RandomOrbAngle(orb, position, distance, lineMultiplier,travelMultiplier);

            yield return new WaitForSeconds(0.5f);
        }

        private void RandomOrbAngle(GameObject orb, Vector2 position, float distance, float lineMultiplier, float travelMultiplier)
        {
            float angle = (float)(rand.NextDouble() * 2 * Math.PI);
            float speed = 37.5f;

            float flippedAngle = (float)((angle + Math.PI) % (2 * Math.PI));

            //angle is reversed so the orb is placed in the opposite direction of velocity
            orb.transform.position = GetPosFromOffsetAngle(position, flippedAngle, distance);

            CreateTeleportLine(orb.transform.position, angle, distance * lineMultiplier);

            StartCoroutine(Velocity(orb, angle, (distance*travelMultiplier)/speed, speed, 0f));
        }

        private Vector2 GetPosFromOffsetAngle(Vector2 orig, float angle, float offsetDistance)
        {
            float posX = (float)(orig.x + (offsetDistance * Math.Cos(angle)));
            float posY = (float)(orig.y + (offsetDistance * Math.Sin(angle)));
            return new Vector2(posX, posY);
        }

        private float GetAngleFromStartToEnd(Vector2 start, Vector2 end)
        {
            Vector2 direction = end - start;

            float angle = (float)Math.Atan2(direction.y, direction.x);

            if (angle < 0)
            {
                angle += (float)(2 * Math.PI);
            }

            return angle;
        }

        private void CreateTeleportLine(Vector2 origin, float angle, float distance)
        {
            GameObject line = Instantiate(teleLine);
            line.transform.position = origin;
            line.transform.SetRotation2D(angle);

            ParticleSystem system = line.GetComponent<ParticleSystem>();
            system.Play();

            StartCoroutine(Velocity(line, angle, distance/160f, 160f,1f));

            StartCoroutine(DisableParticles(system, distance));
        }

        private IEnumerator DisableParticles(ParticleSystem system, float distance)
        {
            yield return new WaitForSeconds(distance / 160f);
            system.Stop();
        }

        private IEnumerator Velocity(GameObject obj, float angle, float lifetime, float speed, float lingerTime)
        {
            float deltatime = 0;

            while (deltatime < lifetime && obj != null)
            {
                deltatime += Time.deltaTime;

                Vector2 newPos = GetPosFromOffsetAngle(obj.transform.position, angle, speed * Time.deltaTime);

                obj.transform.position = newPos;

                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSeconds(lingerTime);
            yield return new WaitForEndOfFrame();
            Destroy(obj);
        }

        private IEnumerator BounceVelocity(GameObject orb, float angle, float lifetime, float speed)
        {
            float deltatime = 0;

            while (deltatime < lifetime)
            {
                deltatime += Time.deltaTime;

                float oldangle = angle;
                angle = Bounce(orb, angle);

                Vector2 newPos = GetPosFromOffsetAngle(orb.transform.position, angle, speed * Time.deltaTime);

                //to prevent bounce spam
                if (oldangle != angle)
                {
                    newPos.x = Mathf.Clamp(newPos.x, 5.1f, 34.9f);
                    newPos.y = Mathf.Clamp(newPos.y, 30.1f, 40.4f);
                }

                orb.transform.position = newPos;

                yield return new WaitForEndOfFrame();
            }
            orb.GetComponent<PlayMakerFSM>().enabled = true;
            orb.GetComponent<PlayMakerFSM>().SetState("Dissipate");
        }

        private float Bounce(GameObject orb, float angle)
        {
            float topY = 40.5f;
            float bottomY = 30f;
            float leftX = 5f;
            float rightX = 35f;

            float orbX = orb.transform.GetPositionX();
            float orbY = orb.transform.GetPositionY();

            bool bounced = false;

            if (orbX < leftX || orbX > rightX)
            {
                angle = (float)(Math.PI - angle);
                bounced = true;
            }

            if (orbY < bottomY || orbY > topY)
            {
                angle = -angle;
                bounced = true;
            }

            if (bounced)
            {
                orb.Child("Impact").SetActive(true);
                orb.GetComponent<AudioSource>().PlayOneShot(orbImpactClip);
                CameraShake();
            }

            return angle;
        }

        private void CameraShake()
        {
            GameCameras.instance.gameObject.Child("CameraParent").GetComponent<PlayMakerFSM>().SendEvent("EnemyKillShake");
        }

        private void BigCameraShake()
        {
            GameCameras.instance.gameObject.Child("CameraParent").GetComponent<PlayMakerFSM>().SendEvent("BigShake");
        }

        private void EndShake()
        {
            GameCameras.instance.gameObject.Child("CameraParent").GetComponent<PlayMakerFSM>().SendEvent("END");
        }

        private void ObjectInit(PlayMakerFSM sovereignControl)
        {
            //mage orb from the fsm
            //you can give it a sprite flash to make it white
            soulOrb = Instantiate(sovereignControl.GetAction<SpawnObjectFromGlobalPool>("Spawn Fireball", 1).gameObject.Value);
            soulOrb.SetActive(false);
            soulOrb.GetComponent<PlayMakerFSM>().GetState("Recycle").InsertAction(0, new DestroySelf { detachChildren = false});
            soulOrb.GetComponent<PlayMakerFSM>().enabled = false;
            soulOrb.AddComponent<SpriteFlash>();
            orbImpactClip = (AudioClip)soulOrb.GetComponent<PlayMakerFSM>().GetAction<AudioPlaySimple>("Impact", 1).oneShotClip.Value;

            teleLine = Instantiate(sovereignControl.GetAction<CreateObject>("Tele Line", 4).gameObject.Value);
            Destroy(teleLine.GetComponent<ParticleSystemAutoDestroy>());
            teleLine.GetComponent<ParticleSystem>().loop = true;

            soulLaser = Instantiate(ResourceLoader.RadianceLaser);
            PlayMakerFSM laserfsm = soulLaser.GetComponent<PlayMakerFSM>();
            laserfsm.GetState("Recycle").RemoveAction(0);
            laserfsm.GetState("Recycle").AddAction(new DestroySelf { detachChildren = false });

            appearFlash = gameObject.Child("Appear Flash");
        }

    }
}
