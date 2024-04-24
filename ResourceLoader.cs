using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Vasi;

namespace SoulSovereign
{
    public class ResourceLoader
    {
        public static GameObject MageKnight;
        public static GameObject MageLord;
        public static GameObject QuakeBlast;
        public static GameObject QuakePillar;
        public static GameObject WhiteFlash;
        public static GameObject AppearFlash;
        public static GameObject FireEffect;
        public static GameObject MageBlob1;
        public static GameObject MageBalloon;
        public static GameObject RadianceLaser;
        public static AudioClip LaserPrepareAudio;
        public static AudioClip LaserFireAudio;
        public static AudioClip RoarAudio;
        public static GameObject StunFX;
        public static GameObject RoarFX;
        public static GameObject WhiteWave;
        public static GameObject DeathParticles;
        //public static GameObject 

        public static void InitResources(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            MageKnight = preloadedObjects["GG_Mage_Knight"]["Mage Knight"].gameObject;
            MageLord = preloadedObjects["GG_Soul_Tyrant"]["Dream Mage Lord"].gameObject;
            MageBlob1 = preloadedObjects["Ruins1_32"]["Mage Blob 1"];
            MageBalloon = preloadedObjects["GG_Mage_Knight_V"]["Balloon Spawner"].Child("Balloons").Child("Mage Balloon Spawner");
            LaserPrepareAudio = (AudioClip)preloadedObjects["GG_Radiance"]["Boss Control/Absolute Radiance"].LocateMyFSM("Attack Commands").GetAction<AudioPlaySimple>("EB 1", 1).oneShotClip.Value;
            LaserFireAudio = (AudioClip)preloadedObjects["GG_Radiance"]["Boss Control/Absolute Radiance"].LocateMyFSM("Attack Commands").GetAction<AudioPlayerOneShotSingle>("EB 1", 2).audioClip.Value;
            RadianceLaser = preloadedObjects["GG_Radiance"]["Boss Control/Absolute Radiance"].LocateMyFSM("Attack Commands").GetAction<ActivateGameObject>("AB Start", 0).gameObject.GameObject.Value.Child("Burst 1").Child("Radiant Beam (3)");
            StunFX = MageLord.LocateMyFSM("Mage Lord").GetAction<SpawnObjectFromGlobalPool>("Stun Init", 4).gameObject.Value;
            RoarAudio = (AudioClip)MageLord.LocateMyFSM("Mage Lord").GetAction<AudioPlaySimple>("Teleport In", 1).oneShotClip.Value;
            RoarFX = MageLord.LocateMyFSM("Mage Lord").GetAction<CreateObject>("Roar", 6).gameObject.Value;
            WhiteWave = preloadedObjects["GG_White_Defender"]["White Defender"].gameObject.Child("Roar Effects");
            DeathParticles = preloadedObjects["GG_White_Defender"]["White Defender"].gameObject.Child("Pt Entry");

            QuakeBlast = MageLord.Child("Quake Blast");
            QuakePillar = MageLord.Child("Quake Pillar");
            WhiteFlash = MageLord.Child("White Flash");
            AppearFlash = MageLord.Child("Appear Flash");
            FireEffect = MageLord.Child("Fire Effect");
        }
    }
}