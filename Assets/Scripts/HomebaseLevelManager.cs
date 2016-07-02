using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using LitJson;
using UnityEngine.SceneManagement;

public class HomebaseLevelManager : MonoBehaviour {

	//UI elements
	public Text supplyText, constructedKnifeText, constructedClubText, constructedAmmoText, constructedGunText, activeSurvivorText, inactiveSurvivorText, cuedWeaponText;
	public Slider currentCraftingProgressSlider;

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

	void UpdateTheUI () {
		supplyText.text = "Supply: " + GameManager.instance.supply.ToString();
		constructedKnifeText.text = "Knives completed: "+ GameManager.instance.knife_for_pickup.ToString();
		constructedClubText.text = "Clubs completed: "+ GameManager.instance.club_for_pickup.ToString();
		constructedAmmoText.text = "Ammo completed: "+ GameManager.instance.ammo_for_pickup.ToString();
		constructedGunText.text = "Gun completed: " + GameManager.instance.gun_for_pickup.ToString();
		cuedWeaponText.text = "Weapons in cue: " + weaponsInCue.ToString();
		activeSurvivorText.text = "Trained Survivors: " + GameManager.instance.active_survivor_for_pickup.ToString();
		inactiveSurvivorText.text = "Untrained Survivors: " + GameManager.instance.inactive_survivors.ToString();
	}

	void UpdateSliderValue () {
			TimeSpan timeUntilFinish = timeActiveWeaponWillComplete - DateTime.Now;

			//Debug.Log("Weapon completes in "+timeActiveWeaponWillComplete.ToString()+" from now that should be: "+timeUntilFinish.TotalSeconds.ToString());

			float secondsToComplete = activeWeaponDuration*60f;
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
	}

//	public bool sendingWeaponCompleteFromClient = false;
//	IEnumerator SendWeaponCompleteFromClient () {
//		if (sendingWeaponCompleteFromClient == false) {
//			sendingWeaponCompleteFromClient = true;
//			WWWForm form = new WWWForm();
//			form.AddField("id", GameManager.instance.userId);
//			form.AddField("entry_id", activeWeaponEntryID);
//			form.AddField("duration", activeWeaponDuration);
//			form.AddField("type", activeWeaponName);
//
//			WWW www = new WWW( clientCallingCompletedWeaponURL , form);
//			yield return www;
//			Debug.Log(www.text);
//
//			if (www.error == null) {
//				string returnString = www.text;
//				JsonData wepCompleteJson = JsonMapper.ToObject(returnString);
//
//				if (wepCompleteJson[0].ToString() == "Success") {
//					Debug.Log(wepCompleteJson[1].ToString());
//					weaponActivelyBeingCrafted = false;
//					StartCoroutine(GetCraftingStatusAndSetCurrentSlider());
//					sendingWeaponCompleteFromClient = false;	
//					yield break;
//				} else if (wepCompleteJson[0].ToString() == "Failed") {
//					Debug.Log(wepCompleteJson[1].ToString());
//				} else {
//					Debug.Log("Server returned a json without success or failure in the index position");
//				}
//			}else{
//				Debug.Log(www.error);
//			}
//		} else {
//			yield break;
//		}

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
				
					if (craftingJson[1].ToString() != "none") {
						weaponsInCue = craftingJson[1].Count-1;
						weaponActivelyBeingCrafted = true;
						DateTime soonestWeaponComplete = DateTime.Parse(craftingJson[1][0]["time_complete"].ToString());
						for (int i = 0; i < craftingJson[1].Count; i++) {
							//find the soonest weapon to complete, and set that complete time for the slider.
							DateTime myDoneTime = DateTime.Parse(craftingJson[1][i]["time_complete"].ToString());
							if (myDoneTime < soonestWeaponComplete) {
								soonestWeaponComplete = myDoneTime;
								activeWeaponName = craftingJson[1][i]["type"].ToString();
								activeWeaponEntryID = (int)craftingJson[1][i]["entry_id"];
								activeWeaponDuration = (int)craftingJson[1][i]["duration"];
							}
						}
						timeActiveWeaponWillComplete = soonestWeaponComplete;
						UpdateTheUI();
					} else {
						weaponsInCue = 0;
						weaponActivelyBeingCrafted = false;
						currentCraftingProgressSlider.gameObject.SetActive(false);
						UpdateTheUI();
					}
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
				GameManager.instance.knife_for_pickup = (int)returnJson[1]["knife_for_pickup"];
				GameManager.instance.club_for_pickup = (int)returnJson[1]["club_for_pickup"];
				GameManager.instance.ammo_for_pickup = (int)returnJson[1]["ammo_for_pickup"];
				GameManager.instance.gun_for_pickup = (int)returnJson[1]["gun_for_pickup"];
				GameManager.instance.active_survivor_for_pickup = (int)returnJson[1]["active_survivor_for_pickup"];
				GameManager.instance.inactive_survivors = (int)returnJson[1]["inactive_survivors"];

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

	public void ConstructWeapon (string type) {
		int cost = 0;
		int dur = 0;
		if (type == "knife") {
			cost = 50;
			dur = 30;
			//check if the user has enough currency
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				int newSup = GameManager.instance.supply;
				UpdateTheUI();
				StartCoroutine(SendCraftStartToServer("knife", cost, dur, newSup));
			}
		}else if (type == "club") {
			cost = 125;
			dur = 240;
			//check if the user has enough currency
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				int newSup = GameManager.instance.supply;
				UpdateTheUI();
				StartCoroutine(SendCraftStartToServer("club", cost, dur, newSup));
			}
		}else if (type == "ammo") {
			cost = 15;
			dur = 15;
			//check if the user has enough currency
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				int newSup = GameManager.instance.supply;
				UpdateTheUI();
				StartCoroutine(SendCraftStartToServer("ammo", cost, dur, newSup));
			}
		}else if (type == "gun") {
			cost = 500;
			dur = 560;
			//check if the user has enough currency
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				int newSup = GameManager.instance.supply;
				UpdateTheUI();
				StartCoroutine(SendCraftStartToServer("gun", cost, dur, newSup));
			}
		}else if (type == "survivor") {
			cost = 2000;
			dur = 600;
			if (GameManager.instance.supply >= cost) {
				GameManager.instance.supply = GameManager.instance.supply - cost;
				int newSup = GameManager.instance.supply;
				StartCoroutine(SendCraftStartToServer("survivor", cost, dur, newSup));
				GameManager.instance.inactive_survivors--;
				UpdateTheUI();
			}
		}else {
			Debug.Log("The string is not being sent correctly from the button");
		}

	}

	public bool sendCraftStarted = false;
	IEnumerator SendCraftStartToServer (string name, int cst, int duration, int newSup) {
		if (sendCraftStarted == false){
			sendCraftStarted = true;
			WWWForm form = new WWWForm();
			form.AddField("id", GameManager.instance.userId);
			form.AddField("type", name);
			form.AddField("cost", cst);
			form.AddField("duration", duration);
			form.AddField("new_supply", newSup);

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
				sendCraftStarted = false;

			} else {
				Debug.Log(www.error);
			}
		} else {

		}
	}
}
