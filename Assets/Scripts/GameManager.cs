﻿using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	#region Variables
	[Header("In Game page")]
	[SerializeField] private TextMeshProUGUI TimeText;
	[SerializeField] private TextMeshProUGUI codeText;
	[SerializeField] private Image[] ImageChoiceObj;
	[Space]
	[Header("Code Character page")]
	[SerializeField] private GameObject canvasCode;
	[SerializeField] private Image imageCharacter;
	[SerializeField] private TextMeshProUGUI textTitle;
	[SerializeField] private TextMeshProUGUI textQuestion;
	[SerializeField] private TextMeshProUGUI textAnswer;
	[Space]
	[Header("Setting")]
	[SerializeField] private CharacterMode[] characterModesSprite;
	[SerializeField] private SpriteGroup[] CharacterSprites;
	[SerializeField] private TextGroup[] QuestionAndAnswerList;

	[Header("Unity Event")]
	[Space]
	public KeyCode key;
	public UnityEvent OnKeyDown;

	private DatabaseReference roomReference;

	private string[] QuestionAndAnswer;
	private long startedTimeTick = 0;
	private string roomToken;

	[Serializable]
	public struct CharacterMode
	{
		[SerializeField] public Sprite[] characterChoice;
	}
	[Serializable]
	public struct SpriteGroup
	{
		[SerializeField] public Sprite[] sprite;
	}
	[Serializable]
	public struct TextGroup
	{
		[SerializeField] public TextAsset[] text;
	}
	#endregion

	#region Core Method
	private void Awake()
	{
		roomToken = PlayerPrefs.GetString("RoomToken");

		FirebaseApp.DefaultInstance.SetEditorDatabaseUrl(Config.FirebaseURL);
		roomReference = FirebaseDatabase.DefaultInstance.GetReference(roomToken);

		roomReference.ChildAdded += HandleChildAdded;
		roomReference.ChildChanged += HandleChildChanged;
		roomReference.ChildRemoved += HandleChildRemoved;

		roomReference.Child("Time").GetValueAsync().ContinueWith(taskGet =>
		{
			if (taskGet.IsCompleted && !taskGet.Result.Exists)
			{
				startedTimeTick = DateTime.Now.Ticks;
				roomReference.Child("Time").SetValueAsync(startedTimeTick);
			}
		});
	}
	private void Update()
	{
		if (startedTimeTick > 0)
		{
			var timSpane = TimeSpan.FromTicks(Config.Instance.TrickFromTimeCapture - (DateTime.Now.Ticks - startedTimeTick));
			TimeText.text = timSpane.ToString(@"hh\ \:\ mm\ \:\ ss");
		}

		if (Input.GetKeyDown(key))
		{
			OnKeyDown.Invoke();
		}
	}

	private void HandleChildAdded(object sender, ChildChangedEventArgs args)
	{
		if (args.DatabaseError != null)
		{
			Debug.LogError(args.DatabaseError.Message);
			return;
		}
		Debug.Log(args.Snapshot.Key);

		switch (args.Snapshot.Key)
		{
			case "Status":
			{
				StartCoroutine(GetStatusAndUpdateChoice());
				IEnumerator GetStatusAndUpdateChoice()
				{
					var modeIndex = 0;
					roomReference.Child("Status").GetValueAsync().ContinueWith(task =>
					{
						if (task.IsCompleted && task.Result.Exists)
						{
							modeIndex = int.Parse(task.Result.Value.ToString());
						}
					});

					yield return new WaitUntil(() => modeIndex != 0);
					modeIndex--;
					for (var i = 0; i < ImageChoiceObj.Length; i++)
					{
						ImageChoiceObj[i].sprite = characterModesSprite[modeIndex].characterChoice[i];
					}
				}
				break;
			}
			case "Time":
			{
				startedTimeTick = long.Parse(args.Snapshot.Value.ToString());
				break;
			}
		}
	}
	private void HandleChildChanged(object sender, ChildChangedEventArgs args)
	{
		if (args.DatabaseError != null)
		{
			Debug.LogError(args.DatabaseError.Message);
			return;
		}

	}
	private void HandleChildRemoved(object sender, ChildChangedEventArgs args)
	{
		if (args.DatabaseError != null)
		{
			Debug.LogError(args.DatabaseError.Message);
			return;
		}

	}
	#endregion

	#region Utils Method
	public void EnterCode()
	{
		var codeName = codeText.text;
		if (codeName.Contains("A") || codeName.Contains("B") || codeName.Contains("C"))
		{
			if (codeName[1].Equals("0"))
			{
				var n = int.Parse(codeName[2].ToString());
				if (n > 0 && n < 9)
				{
					var firstLetter = codeName[0].ToString();
					var arrayIdx = -1;
					switch (firstLetter)
					{
						case "A":
							arrayIdx = 0;
							break;
						case "B":
							arrayIdx = 1;
							break;
						case "C":
							arrayIdx = 2;
							break;
					}
					imageCharacter.sprite = CharacterSprites[arrayIdx].sprite[n - 1];
					QuestionAndAnswer = QuestionAndAnswerList[arrayIdx].text[n - 1].text.Split( new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
					textTitle.text = QuestionAndAnswer[0];
					canvasCode.SetActive(true);
				}
			}
		}
		codeText.text = "Input code here";
	}
	public void AddCode(string letter)
	{
		codeText.text += letter;
	}
	public void ClearCode()
	{
		codeText.text = "";
	}
	public void DeleteLastCode()
	{
		codeText.text = codeText.text.Remove(codeText.text.Length - 1);
	}
	public void RandomQuestionAndAnswer()
	{
		var QuestionRandomNumber = UnityEngine.Random.Range(1, 9);
		QuestionRandomNumber = (QuestionRandomNumber * 2) - 1;
		textQuestion.gameObject.transform.parent.gameObject.SetActive(false);
		textAnswer.gameObject.transform.parent.gameObject.SetActive(false);

		textQuestion.text = QuestionAndAnswer[QuestionRandomNumber];
		textAnswer.text = QuestionAndAnswer[QuestionRandomNumber + 1];
	}
	#endregion
}
