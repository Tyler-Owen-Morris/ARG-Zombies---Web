using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using LitJson;

public class QRPanelController : MonoBehaviour {

	public QRCodeEncodeController e_qrController;
	public RawImage qrCodeImage;
	public string qrEncodeString;

	// Use this for initialization
	void Start () {
		if (e_qrController != null) {
			e_qrController.onQREncodeFinished += qrEncodeFinished;//Add Finished Event
		}
	}
	
	void qrEncodeFinished(Texture2D tex)
	{
		if (tex != null && tex != null) {
			qrCodeImage.texture = tex;
		} else {

		}
	}

	public void Encode()
	{
		if (e_qrController != null) {
			e_qrController.onQREncodeFinished += qrEncodeFinished;
			string valueStr = qrEncodeString;
			e_qrController.Encode(valueStr);
		}
	}

	public void ConstructAndEncodeQR () {
		string[] myEncodeArray = new string[4];
		myEncodeArray[0] = "homebase";
		myEncodeArray[1] = GameManager.instance.userId;
		myEncodeArray[2] = GameManager.instance.homebase_lat.ToString();
		myEncodeArray[3] = GameManager.instance.homebase_lon.ToString();

		string myResult = JsonMapper.ToJson(myEncodeArray);
		Debug.Log(myResult);
		qrEncodeString = myResult;
		Encode();
	}
}
