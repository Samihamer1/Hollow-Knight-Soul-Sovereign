using System.Collections;

namespace SoulSovereign
{
    public class SoulSovereign : Mod
    {
        public SoulSovereign() : base("Soul Sovereign") { }
        public override string GetVersion() => "v1.0.0.0";
        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Mage_Knight","Mage Knight"),
                ("GG_Soul_Tyrant", "Dream Mage Lord"),
                ("GG_Radiance","Boss Control/Absolute Radiance"),
                ("Ruins1_32", "Mage Blob 1"),
                ("GG_Mage_Knight_V", "Balloon Spawner"),
                ("GG_White_Defender", "White Defender")
            };
        }
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            ResourceLoader.InitResources(preloadedObjects);
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += CheckScene;
            ModHooks.LanguageGetHook += ChangeText;
        }

        private string ChangeText(string key, string sheetTitle, string orig)
        {
            if (key == "NAME_SOUL_TYRANT")
            {
                return "Soul Sovereign";
            }
            if (key == "GG_S_SOUL_TYRANT")
            {
                return "Mad god of sorcery";
            }
            if (key == "MAGE_LORD_DREAM_MAIN")
            {
                return "Sovereign";
            }
            if (key == "MAGELORD_D_1")
            {
                return "That toy has no power over me.";
            }
            if (key == "MAGELORD_D_2")
            {
                return "Accursed blade! Out of my mind!";
            }
            if (key == "MAGELORD_D_3")
            {
                return "True Focus casts aside even the body.";
            }
            return orig;
        }


        private void CheckScene(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            if (arg1.name == "GG_Soul_Tyrant")
            {
                GameManager.instance.StartCoroutine(WaitForBoss());
            }
        }

        private IEnumerator WaitForBoss()
        {
            yield return new WaitWhile(() => GameObject.Find("Dream Mage Lord") == null);
            GameObject.Find("Dream Mage Lord").AddComponent<Sovereign>();
            yield return new WaitWhile(() => GameObject.Find("Dream Mage Lord Phase2") == null);
            GameObject Sovereign3 = UnityEngine.Object.Instantiate(GameObject.Find("Dream Mage Lord Phase2"));
            Sovereign3.SetActive(false);
            Sovereign3 sov3 = Sovereign3.AddComponent<Sovereign3>();
            Sovereign2 sov2 = GameObject.Find("Dream Mage Lord Phase2").AddComponent<Sovereign2>();
            sov2.SetSovereign3(sov3);
        }
    }
}