using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using LitJson;
using UnityEngine.SceneManagement;

public class HomebaseLevelManager : MonoBehaviour {

	//UI elements
	public Text supplyText, constructedKnifeText, constructedClubText, constructedAmmoText, constructedGunText, activeSurvivorText, inactiveSurvivorText, cuedWeaponText, sliderClockText;
	public Slider currentCraftingProgressSlider;
	public GameObject QRPanel;

	//numbers for calculating the active weapon
	private DateTime timeActiveWeaponWillComplete;
	private string activeWeaponName;
	public int activeWeaponDuration, activeWeaponEntryID, weaponsInCue;
	private bool weaponActivelyBeingCrafted = false;

	private string getSupplyURL = "http://www.argzombie.com/ARGZ_SERVER/Homebase_GetSupply.php";
	private string startCraftingURL = "http://www.argzombie.com/ARGZ_SERVER/Homebase_StartCrafting.php";
	private string getCraftingStatusURL = "http://www.argzombie.com/ARGZ_SERVER/Homebase_GetCraftingStatus.php";
	private string getStatusURL = "http://www.argzombie.com/ARGZ_SERVER/Homebase_GetStatus.php";
	private string clientCallingCompletedWeaponURL = "http://www.argzombie.com/ARGZ_SERVER/Homebase_ClientCallingCraftComplete.php";

	void Start () {
		UpdateTheUI();
		InvokeRepeating("UpdateDataFromServer", 0f, 10f);
	}

	void Update () {
		if (weaponActivelyBeingCrafted == true) {
			if (currentCraftingProgressSlider.gameObject.activeInHierarchy == false ) {
				currentCraftingProgressSlider.gameObject.SetActive(true);
			}
			UpdateSliderValue();
		} else {
			currentCraftingProgressSlider.gameObject.SetActive(false);
		}
	}

	public void QRPanelOpened () {
		QRPanel.GetComponent<QRPanelController>().ConstructAndEncodeQR();
		QRPanel.SetActive(true);
	}

	public void QRPanelClose () {
		QRPanel.SetActive(false);
	}

	void UpdateTheUI () {
		supplyText.text = "Supply: " + GameManager.instance.supply.ToString();
		cuedWeaponText.text = "Weapons in cue: " + weaponsInCue.ToString();
//		constructedKnifeText.text = "Knives completed: "+ GameManager.instance.knife_for_pickup.ToString();
//		constructedClubText.text = "Clubs completed: "+ GameManager.instance.club_for_pickup.ToString();
//		constructedAmmoText.text = "Ammo completed: "+ GameManager.instance.ammo_for_pickup.ToString();
//		constructedGunText.text = "Gun completed: " + GameManager.instance.gun_for_pickup.ToString();
//		activeSurvivorText.text = "Trained Survivors: " + GameManager.instance.active_survivor_for_pickup.ToString();
//		inactiveSurvivorText.text = "Untrained Survivors: " + GameManager.instance.inactive_survivors.ToString();
	}

	void UpdateSliderValue () {
			TimeSpan timeUntilFinish = timeActiveWeaponWillComplete - DateTime.Now;

			//Debug.Log("Weapon completes in "+timeActiveWeaponWillComplete.ToString()+" from now that should be: "+timeUntilFinish.TotalSeconds.ToString());

			float secondsToComplete = activeWeaponDuration*60.0f;
			//Debug.Log("Seconds to complete: "+secondsToComplete.ToString());
			double inverseSliderValue = timeUntilFinish.TotalSeconds / secondsToComplete;
			double sliderValue = 1.0f - inverseSliderValue;
			//Debug.Log("slider value should be " + sliderValue.ToString());
			if (sliderValue <= 1.0f) {
				currentCraftingProgressSlider.value = (float)sliderValue;
			} else {
				currentCraftingProgressSlider.value = 0.0f;
				weaponActivelyBeingCrafted = false;
				StartCoroutine(GetCraftingStatusAndSetCurrentSlider());
			}

			//declare the string to construct from the timespan
			string myClockText = "";
			if (timeUntilFinish.Hours > 0) {
				myClockText += timeUntilFinish.Hours.ToString()+":";
				timeUntilFinish = timeUntilFinish - TimeSpan.FromHours(timeUntilFinish.Hours);
			}
			if(timeUntilFinish.Minutes > 0){
				myClockText += timeUntilFinish.Minutes.ToString()+":";
				timeUntilFinish = timeUntilFinish - TimeSpan.FromMinutes(timeUntilFinish.Minutes);
			}
			if(timeUntilFinish.Seconds > 0) {
				myClockText += timeUntilFinish.Seconds.ToString()+"s";
			}
			sliderClockText.text = myClockText;
		}


	void UpdateDataFromServer () {
		StartCoroutine(GetCraftingStatusAndSetCurrentSlider());
		StartCoroutine(UpdateStatsAndTextFromServer());
	}


	IEnumerator GetCraftingStatusAndSetCurrentSlider () {
		WWWForm form = new WWWForm();
		form.AddField("id", GameManager.instance.userId);

		WWW www = new WWW( getCraftingStatusURL, form);
		yield return www;
		Debug.Log(www.text);

		if (www.error == null) {
			string returnString = www.text;
			JsonData craftingJson = JsonMapper.ToObject(returnString);

			if (craftingJson[0].ToString() == "Success") {

				//process the array with the weapons in progress
				if (craftingJson[1].Count > 0) {
					weaponsInCue = craftingJson[1].Count;
					weaponActivelyBeingCrafted = true;
					DateTime soonestWeaponComplete = DateTime.Parse(craftingJson[1][0]["time_complete"].ToString());
					activeWeaponName = craftingJson[1][0]["type"].ToString();
					activeWeaponEntryID = (int)craftingJson[1][0]["entry_id"];
					activeWeaponDuration = (int)craftingJson[1][0]["duration"];
					for (int i = 0; i < craftingJson[1].Count; i++) {
						//find the soonest weapon to complete, and set that complete time for the slider.
						DateTime myDoneTime = DateTime.Parse(craftingJson[1][i]["time_complete"].ToString());
						if (myDoneTime < soonestWeaponComplete) {
							soonestWeaponComplete = myDoneTime;
							activeWeaponName = craftingJson[1][i]["type"].ToString();
							activeWeaponEntryID = (int)craftingJson[1][i]["entry_id"];
							activeWeaponDuration = (int)craftingJson[1][i]["duration"];
							Debug.Log(activeWeaponDuration.ToString());
						}
					}
					Debug.Log("active weapon complete: "+soonestWeaponComplete.ToString()+" active weapon duration: "+activeWeaponDuration.ToString());
					timeActiveWeaponWillComplete = soonestWeaponComplete;
					UpdateTheUI();
				} else {
					weaponsInCue = 0;
					weaponActivelyBeingCrafted = false;
					currentCraftingProgressSlider.gameObject.SetActive(false);

				}

				//process the array with the completed weapons
				if (craftingJson[2].Count > 0) {
					for (int i = 0; craftingJson[2].Count > i; i++) {
						if (craftingJson[2][i]["type"].ToString() == "shiv") {
							GameManager.instance.knife_for_pickup ++;
						} else if (craftingJson[2][i]["type"].ToString() == "hunting knife") {
							GameManager.instance.knife_for_pickup ++;
						}else if (craftingJson[2][i]["type"].ToString() == "baseball bat") {
							GameManager.instance.club_for_pickup ++;
						}else if (craftingJson[2][i]["type"].ToString() == "sledgehammer") {
							GameManager.instance.club_for_pickup ++;
						}else if (craftingJson[2][i]["type"].ToString() == ".22 revolver") {
							GameManager.instance.gun_for_pickup ++;
						}else if (craftingJson[2][i]["type"].ToString() == "shotgun") {
							GameManager.instance.gun_for_pickup ++;
						}
					}
				}


				UpdateTheUI();

			} else if (craftingJson[0].ToString() == "Failed") {
				Debug.Log(craftingJson[1].ToString());
			} else {
				Debug.Log("Attempting to update crafting status- json did not return valid success or failure");
			}
		} else {
			Debug.Log(www.error);
		}
	}

	IEnumerator UpdateStatsAndTextFromServer () {
		WWWForm form = new WWWForm();
		form.AddField("id", GameManager.instance.userId);

		WWW www = new WWW(getStatusURL, form);
		yield return www;
		Debug.Log(www.text);

		if (www.error == null) {
			string returnString = www.text;
			JsonData returnJson = JsonMapper.ToObject(returnString);

			if (returnJson[0].ToString() == "Success") {
				GameManager.instance.supply = (int)returnJson[1]["supply"];
//				GameManager.instance.knife_for_pickup = (int)returnJson[1]["knife_for_pickup"];
//				GameManager.instance.club_for_pickup = (int)returnJson[1]["club_for_pickup"];
//				GameManager.instance.ammo_for_pickup = (int)returnJson[1]["ammo_for_pickup"];
//				GameManager.instance.gun_for_pickup = (int)returnJson[1]["gun_for_pickup"];
//				GameManager.instance.active_survivor_for_pickup = (int)returnJson[1]["active_survivor_for_pickup"];
//				GameManager.instance.inactive_survivors = (int)returnJson[1]["inactive_survivors"];

				UpdateTheUI();
			} else if (returnJson[0].ToString() == "Failed") {
				Debug.Log(returnJson[1].ToString());
			} else {
				Debug.Log("Json object did not return a valid json success or failure response");
			}
		} else {
			Debug.Log(www.error);
		}
	}

	public void BackButtonPressed () {
		SceneManager.LoadScene("01a Login");
	}

	public void ConstructWeapon (string wepName) {
		int cost = 0;
		int dur = 0;
		int wep_index =0;
		if (wepName == "shiv") {
			cost = 20;
			dur = 4;
			wep_index = 1;
			//check if the user has enough currency
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				weaponsInCue++;
				StartCoroutine(SendCraftStartToServer(wepName, cost, dur, wep_index));
			}
		}else if (wepName == "hunting knife") {
			cost = 100;
			dur = 45;
			wep_index = 4;
			//check if the user has enough currency
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				weaponsInCue++;
				StartCoroutine(SendCraftStartToServer(wepName, cost, dur, wep_index));
			}
		}else if (wepName == "baseball bat") {
			cost = 125;
			dur = 60;
			wep_index = 2;
			//check if the user has enough currency
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				weaponsInCue++;
				StartCoroutine(SendCraftStartToServer(wepName, cost, dur, wep_index));
			}
		}else if (wepName == "sledgehammer") {
			cost = 150;
			dur = 120;
			wep_index = 5;
			//check if the user has enough currency
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				StartCoroutine(SendCraftStartToServer(wepName, cost, dur, wep_index));
			}
		}else if (wepName == ".22 revolver") {
			cost = 500;
			dur = 220;
			wep_index = 3;
			//check if the user has enough currency
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				weaponsInCue++;
				StartCoroutine(SendCraftStartToServer(wepName, cost, dur, wep_index));
			}
		}else if (wepName == "shotgun") {
			cost = 1000;
			dur = 560;
			wep_index = 3;
			//check if the user has enough currency
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				weaponsInCue++;
				StartCoroutine(SendCraftStartToServer(wepName, cost, dur, wep_index));
			}
		}else if (wepName == "ammo") {
			cost = 10;
			dur = 1;
			wep_index = 0;
			//check if the user has enough currency
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				weaponsInCue++;
				StartCoroutine(SendCraftStartToServer("ammo", cost, dur, wep_index));
			}
		}else {
			Debug.Log("The string is not being sent correctly from the button");
		}
		UpdateTheUI();
//		}else if (wepName == "survivor") {
//			cost = 2000;
//			dur = 600;
//			if (GameManager.instance.supply >= cost) {
//				GameManager.instance.supply = GameManager.instance.supply - cost;
//				int newSup = GameManager.instance.supply;
//				StartCoroutine(SendCraftStartToServer(wepName, cost, dur, wep_index));
//				GameManager.instance.inactive_survivors--;
//				UpdateTheUI();
//			}
//		}


	}

	IEnumerator SendCraftStartToServer (string wep_name, int cst, int duration, int weapon_index) {
		WWWForm form = new WWWForm();
		form.AddField("id", GameManager.instance.userId);
		form.AddField("wep_name", wep_name);
		form.AddField("cost", cst);
		form.AddField("duration", duration);
		form.AddField("weapon_index", weapon_index);


		WWW www = new WWW( startCraftingURL, form);
		yield return www;

		if (www.error == null) {
			string returnString = www.text;
			Debug.Log(returnString);
			JsonData returnJson = JsonMapper.ToObject(returnString);

			if (returnJson[0].ToString() == "Success") {
				Debug.Log(returnJson[1].ToString());
			} else if (returnJson[0].ToString() == "Failed") {
				Debug.Log(returnJson[1].ToString());
			} else {
				Debug.Log("json returned something other than success or failure");
			}
		} else {
			Debug.Log(www.error);
		}
		
	}
}
