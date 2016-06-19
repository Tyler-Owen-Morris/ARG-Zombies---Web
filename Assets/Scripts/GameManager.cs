using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using Facebook.Unity;
using LitJson;
using System.IO;

public class GameManager : MonoBehaviour {

	public static GameManager instance;
	private Scene activeScene;

	public int survivorsActive, totalSurvivors, daysSurvived, supply, reportedSupply, reportedWater, reportedFood, reportedTotalSurvivor, reportedActiveSurvivor, playerCurrentHealth, zombiesToFight, shivCount, clubCount, gunCount, foodCount, waterCount, mealCount;
	public DateTime timeCharacterStarted;
	public float homebaseLat, homebaseLong;
	public string userId;
	public string userFirstName;
	public string userLastName;
	public string locationJsonText, clearedBldgJsonText;

	public List <GameObject> survivorCardList = new List<GameObject>();

	//private string startNewCharURL = "http://www.argzombie.com/ARGZ_SERVER/StartNewCharacter.php";
	private string resumeCharacterUrl = "http://www.argzombie.com/ARGZ_SERVER/ResumeCharacter.php";
//	private string updateAllStatsURL = "http://www.argzombie.com/ARGZ_SERVER/UpdateAllPlayerStats.php";
//	private string buildingClearedURL = "http://www.argzombie.com/ARGZ_SERVER/NewBuildingCleared.php";
	private string clearedBuildingDataURL = "http://www.argzombie.com/ARGZ_SERVER/ClearedBuildingData.php";
	private string fetchSurvivorDataURL = "http://www.argzombie.com/ARGZ_SERVER/FetchSurvivorData.php";


	private static SurvivorPlayCard survivorPlayCardPrefab;

	void Awake () {
		MakeSingleton();

		survivorPlayCardPrefab = Resources.Load<SurvivorPlayCard>("Prefabs/SurvivorPlayCard");
	}

	void OnLevelWasLoaded () {
		//this is a catch all to slave the long term memory to the active GameManager.instance object- each load will update long term memory.


		activeScene = SceneManager.GetActiveScene();
		if (activeScene.name.ToString() == "02a Homebase"){
			
		} else if (activeScene.name.ToString() == "01a Login") {
			LoginManager loginMgr = FindObjectOfType<LoginManager>();
			if (FB.IsLoggedIn == true) {
				loginMgr.loggedInPanel.SetActive(true);
			}
		}
	}

	void MakeSingleton() {
		if (instance != null) {
			Destroy (gameObject);
		} else {
			instance = this;
			DontDestroyOnLoad (gameObject);
		}
	}

	public void ResumeGame () {
		StartCoroutine(FetchResumePlayerData());
		StartCoroutine(FetchSurvivorData());
	}
	
	IEnumerator FetchResumePlayerData () {
		WWWForm form = new WWWForm();
		if (FB.IsLoggedIn == true) {
			form.AddField("id", GameManager.instance.userId);
		} else {
			GameManager.instance.userId = "10154194346243929";
			form.AddField("id", GameManager.instance.userId);
		}

		WWW www = new WWW(resumeCharacterUrl, form);
		yield return www;

		if (www.error == null) {

			Debug.Log ("resuming character, server returned raw json string of: " + www.text);

			//write the raw WWW return to a .json file 
			//File.WriteAllText(Application.dataPath + "/Resources/Player.json", www.text.ToString());

			//read that text out into a string object, and map that to a json object
			string playerJsonString = www.text.ToString();
			JsonData playerJson = JsonMapper.ToObject(playerJsonString);


			//update the GameManager.instance with all dataum
			GameManager.instance.userFirstName = playerJson["first_name"].ToString() ;
			GameManager.instance.userLastName = playerJson["last_name"].ToString();
			int totsuv = Convert.ToInt32(playerJson["total_survivors"].ToString());
			GameManager.instance.totalSurvivors = totsuv;
			int suvAct = Convert.ToInt32(playerJson["active_survivors"].ToString());
			GameManager.instance.survivorsActive = suvAct;
			int currHealth = Convert.ToInt32(playerJson["last_player_current_health"].ToString());
			GameManager.instance.playerCurrentHealth = currHealth;
			int sup = Convert.ToInt32(playerJson["supply"].ToString());
			GameManager.instance.supply = sup;
			int wat = Convert.ToInt32(playerJson["water"].ToString());
			GameManager.instance.waterCount = wat;
			int fud = Convert.ToInt32(playerJson["food"].ToString());
			GameManager.instance.foodCount = fud;
			int meal = Convert.ToInt32(playerJson["meals"].ToString());
			GameManager.instance.mealCount = meal;
			int knifeC = Convert.ToInt32(playerJson["knife_count"].ToString());
			GameManager.instance.shivCount = knifeC;
			int clubC = Convert.ToInt32(playerJson["club_count"].ToString());
			GameManager.instance.clubCount = clubC;
			int gunC = Convert.ToInt32(playerJson["gun_count"].ToString());
			GameManager.instance.gunCount = gunC;
			float homeLat = (float)Convert.ToDouble(playerJson["homebase_lat"].ToString());
			GameManager.instance.homebaseLat = homeLat;
			float homeLon = (float)Convert.ToDouble(playerJson["homebase_lon"].ToString());
			GameManager.instance.homebaseLong = homeLon;
			Debug.Log ("server returned a date time string of: " + playerJson["char_created_DateTime"]);
			DateTime oDate = Convert.ToDateTime(playerJson["char_created_DateTime"].ToString());
			GameManager.instance.timeCharacterStarted = oDate;

			//once the GameManager.instance is updated- you're clear to load the map level.
//			if (SceneManager.GetActiveScene().buildIndex != 2 ) {
//				SceneManager.LoadScene("02a Map Level");
//			}

			yield break;
		} else {
			Debug.Log ("WWW error" + www.error);
		}

	}

	IEnumerator FetchSurvivorData () {
		//construct form
		WWWForm form = new WWWForm();
		if (FB.IsLoggedIn == true) {
			form.AddField("id", GameManager.instance.userId);
		} else {
			GameManager.instance.userId = "10154194346243929";
			form.AddField("id", GameManager.instance.userId);
		}
		//make www call
		WWW www = new WWW(fetchSurvivorDataURL, form);
		yield return www;
		Debug.Log(www.text);

		if (www.error == null) {
			//encode json return
			string survivorJsonString = www.text;
			JsonData survivorJson = JsonMapper.ToObject(survivorJsonString);

			if (survivorJson[0].ToString() != "Failed") {
				//parse through json creating "player cards" within gamemanager for each player found on the server.
				for (int i = 0; i < survivorJson.Count; i++) {
					SurvivorPlayCard instance = Instantiate(survivorPlayCardPrefab);
					instance.survivor.name = survivorJson[i]["name"].ToString();
					instance.gameObject.name = survivorJson[i]["name"].ToString();
					//instance.survivor.weaponEquipped.name = survivorJson[i]["weapon_equipped"].ToString();
					instance.survivor.baseAttack = (int)survivorJson[i]["base_attack"];
					instance.survivor.baseStamina = (int)survivorJson[i]["base_stam"];
					instance.survivor.curStamina = (int)survivorJson[i]["curr_stam"];
					instance.entry_id = (int)survivorJson[i]["entry_id"];
					instance.survivor_id = (int)survivorJson[i]["survivor_id"];

					instance.transform.SetParent(GameManager.instance.transform);
				}
				survivorCardList.AddRange (GameObject.FindGameObjectsWithTag("survivorcard"));
			} else {
				//server has returned a failure
				Debug.Log("Survivor Query failed: "+survivorJson[1].ToString());
			}


			if (SceneManager.GetActiveScene().buildIndex != 2 ) {
				SceneManager.LoadScene("02a Homebase");
			}

		} else {
			Debug.LogWarning(www.error);
		}
	}

}
