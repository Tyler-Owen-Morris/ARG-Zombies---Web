using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using LitJson;
using UnityEngine.SceneManagement;

public class HomebaseLevelManager : MonoBehaviour {

	public Text supplyText;

	private string getSupplyURL = "http://www.argzombie.com/ARGZ_SERVER/Homebase_GetSupply.php";

	void Start () {
		supplyText.text = "Supply: " + GameManager.instance.supply.ToString();
		StartCoroutine(PeriodicallyUpdateSupplyTextFromServer());

	}

	IEnumerator PeriodicallyUpdateSupplyTextFromServer () {
		WWWForm form = new WWWForm();
		form.AddField("id", GameManager.instance.userId);

		WWW www = new WWW(getSupplyURL, form);
		yield return www;
		Debug.Log(www.text);

		if (www.error == null) {
			string supplyReturnText = www.text.ToString();
			JsonData supplyJson = JsonMapper.ToObject(supplyReturnText);

			if (supplyJson[0].ToString() == "Success") {
				int supply = (int)supplyJson[1];
				GameManager.instance.supply = supply;
				supplyText.text = "Supply: " + GameManager.instance.supply.ToString();
			}

		} else {
			Debug.Log(www.error);
		}

		Debug.Log("finished updating supply text from server");
		yield return new WaitForSeconds(15f);
		StartCoroutine(PeriodicallyUpdateSupplyTextFromServer());
		yield break;
	}

	public void BackButtonPressed () {
		SceneManager.LoadScene("01a Login");
	}
}
