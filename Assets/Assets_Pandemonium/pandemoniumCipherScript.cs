using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using PlanarCipherWords;
using Rnd = UnityEngine.Random;
public class pandemoniumCipherScript : MonoBehaviour {

	public KMAudio Audio;
	public AudioClip[] Sounds;
	public KMSelectable[] Button;
	public GameObject Rock;
	public TextMesh[] Text;
	public KMBombModule Module;

	private bool solved;
	private bool[] isanimating = new bool[2];
	private string[] words = new string[4];
	private string[] displays1 = new string[2];
	private string[] displays2 = new string[2];
	private int[] wordindex = new int[2];
	private string encrypted;
	private int mazepos;
	private string input = "";
	private char keyletter;
	
	static int _moduleIdCounter = 1;
	int _moduleID = 0;

	private KMSelectable.OnInteractHandler PanelPressed(int pos)
	{
		return delegate
		{
			Button[pos].AddInteractionPunch();
			if (!solved)
			{
				Audio.PlaySoundAtTransform("RockClack", Module.transform);
				if (pos < 2)
				{
					StartCoroutine(Cycle(pos));
					if (pos == 0)
						input = "";
				}
				else if (pos < 6)
				{
					switch (pos - 2)
					{
						case 0:
							//up
							mazepos = (mazepos + 20) % 25;
							break;
						case 1:
							//down
							mazepos = (mazepos + 5) % 25;
							break;
						case 2:
							//left
							mazepos = (mazepos % 5 + 4) % 5 + mazepos / 5 * 5;
							break;
						case 3:
							//right
							mazepos = (mazepos % 5 + 1) % 5 + mazepos / 5 * 5;
							break;
					}
					for (int i = 0; i < 25; i++)
						if (i == mazepos)
							Text[i + 14].color = new Color32(255, 255, 255, 255);
						else
							Text[i + 14].color = new Color32(34, 34, 34, 255);
				}
				else
				{
					string alphabet = "abcdefghijklmnopqrstuvwyz";
					if (input.Length < 7 && !isanimating[0])
					{
						if (input.Length != 7)
							input += alphabet[mazepos];
						for (int j = 0; j < 6; j++)
							Text[j].text = Text[j + 1].text;
						Text[6].text = input.Last().ToString().ToUpperInvariant();
						if (input.Length == 7)
						{
							if (input == words[0].ToLowerInvariant().Replace('x', keyletter))
							{
								solved = true;
								StartCoroutine(Shutdown());
							}
							else
							{
								Debug.LogFormat("[Pandemonium Cipher #{0}] Submitted {1}, but I expected {2}.", _moduleID, input.ToUpperInvariant(), words[0].ToUpperInvariant().Replace('X', keyletter).ToUpperInvariant());
								Module.HandleStrike();
								input = "";
								StartCoroutine(Ouch());
							}
						}
					}
				}
			}
			return false;
		};
	}

	void Awake () {
		_moduleID = _moduleIdCounter++;

		for (int i = 0; i < Button.Length; i++)
			Button[i].OnInteract += PanelPressed(i);
		for (int i = 0; i < words.Length; i++)
			words[i] = Wordlist.wordlist[Rnd.Range(0, Wordlist.wordlist.Length)];
		keyletter = "abcdefghijklmnopqrstuvwyz".PickRandom();
		PandemoniumEncrypt(words[0], words[1], words[2], words[3], keyletter);
		displays1 = new string[] { encrypted, words[1] };
		displays2 = new string[] { words[2], words[3] };
		for (int i = 0; i < 7; i++)
			Text[i].text = displays1[0][i].ToString().ToUpperInvariant();
		for (int i = 0; i < 7; i++)
			Text[i + 7].text = displays2[0][i].ToString().ToUpperInvariant();
		Text[39].text = keyletter.ToString().ToUpperInvariant() + " Sub";
	}

	private void PandemoniumEncrypt(string word, string keyword, string keyword2, string keyword3, char keyletter)
	{
		word = word.ToLowerInvariant();
		Debug.LogFormat("[Pandemonium Cipher #{0}] Encrypted word is {1}.", _moduleID, word.ToUpperInvariant());
		string alphabet = "abcdefghijklmnopqrstuvwyz";
		string key = "";
		int[] keypos = new int[25];
		string encryption = "";

		//keyword to key
		string keywordbk = keyword.ToLowerInvariant().Replace("x", "");
		while (keypos.Any(x => x == 0))
			for (int i = 0; i < keywordbk.Length; i++)
				for (int j = 0; j < 25; j++)
					if (keywordbk[i] == alphabet[j])
						for (int k = 0; k < 25; k++)
							if (keypos[(j + k) % 25] == 0)
							{
								keypos[(j + k) % 25] = keypos.Count(x => x != 0) + 1;
								k = 25;
								j = 25;
							}
		for (int i = 0; i < 25; i++)
			key += alphabet[keypos[i] - 1];
		Debug.LogFormat("[Pandemonium Cipher #{0}] Key made from {1} is {2}.", _moduleID, keyword.ToUpperInvariant(), key.ToUpperInvariant());

		//step 3
		string wordbk = word.Where(x => x != 'x').Join("");
		for (int i = 0; i < wordbk.Length; i++)
			for (int j = 0; j < 25; j++)
				if (key[j] == (keyletter.ToString() + wordbk)[i])
					for (int k = 0; k < 25; k++)
						if (key[k] == wordbk[i])
							encryption += key[3 * (j / 5 + k / 5) % 5 * 5 + 3 * (j % 5 + k % 5) % 5];
		wordbk = "";
		for (int i = 0; i < 7; i++)
			if (word[i] == 'x')
				wordbk += 'x';
			else
				wordbk += encryption[i - word.Substring(0, i).Count(x => x == 'x')];
		Debug.LogFormat("[Pandemonium Cipher #{0}] Encryption of step 3 using keyletter {1} results in {2}.", _moduleID, keyletter.ToString().ToUpperInvariant(), wordbk.ToUpperInvariant());

		//step 2
		encryption = "";
		string keywdbk1 = keyword2.ToLowerInvariant().Replace('x', keyletter);
		string keywdbk2 = keyword3.ToLowerInvariant().Replace('x', keyletter);
		for (int i = 0; i < wordbk.Length; i++)
			if (wordbk[i] == 'x')
				encryption += 'x';
			else
				for (int j = 0; j < 25; j++)
					if (key[j] == keywdbk1[i])
						for (int k = 0; k < 25; k++)
							if (key[k] == keywdbk2[i])
								for (int l = 0; l < 25; l++)
									if (key[l] == wordbk[i])
										encryption += key[(j / 5 + k / 5 + 5 - l / 5) % 5 * 5 + (j % 5 + k % 5 + 5 - l % 5) % 5];
		wordbk = encryption;
		Debug.LogFormat("[Pandemonium Cipher #{0}] Encryption of step 2 using keyletter {1} and keywords {2} and {3} results in {4}.", _moduleID, keyletter.ToString().ToUpperInvariant(), keyword2.ToUpperInvariant(), keyword3.ToUpperInvariant(), wordbk.ToUpperInvariant());

		//step 1
		encryption = "";
		int keyltrpos = 0;
		for (int i = 0; i < 25; i++)
			if (key[i] == keyletter)
				keyltrpos = i;
		for (int i = 0; i < wordbk.Length; i++)
			if (wordbk[i] == 'x')
				encryption += 'x';
			else
				for (int j = 0; j < 25; j++)
					if (key[j] == wordbk[i])
						encryption += key[(j / 5 - keyltrpos / 5 + 7) % 5 * 5 + (j % 5 - keyltrpos % 5 + 7) % 5];
		encrypted = encryption;
		Debug.LogFormat("[Pandemonium Cipher #{0}] Encryption of step 1 using keyletter {1} results in {2}.", _moduleID, keyletter.ToString().ToUpperInvariant(), encryption.ToUpperInvariant());
	}

	private IEnumerator Cycle(int pos)
	{
		while (isanimating[pos])
		{
			yield return null;
		}
		isanimating[pos] = true;
		wordindex[pos] = 1 - wordindex[pos];
		for (int i = 0; i < 7; i++)
		{
			for (int j = 0; j < 6; j++)
				Text[j + pos * 7].text = Text[j + pos * 7 + 1].text;
			if (pos == 0)
				Text[6].text = displays1[wordindex[pos]][i].ToString().ToUpperInvariant();
			else
				Text[13].text = displays2[wordindex[pos]][i].ToString().ToUpperInvariant();
			yield return new WaitForSeconds(0.1f);
		}
		isanimating[pos] = false;
	}

	private IEnumerator Shutdown()
	{
		for (int i = 0; i < 2; i++)
		{
			displays1[i] = "       ";
			displays2[i] = "       ";
		}
		for (int i = 0; i < 2; i++)
			StartCoroutine(Cycle(i));
		for (int i = 0; i < 10; i++)
		{
			Rock.transform.localPosition += new Vector3(0, 0.001f, 0);
			yield return new WaitForSeconds(0.03f);
		}
		Module.HandlePass();
		while (true)
		{
			Rock.transform.localEulerAngles += new Vector3(0, 1f, 0);
			yield return new WaitForSeconds(0.03f);
		}
	}

	private IEnumerator Ouch()
	{
		for (int i = 0; i < 3; i++)
		{
			Rock.transform.localEulerAngles += new Vector3(0, 73f, 0);
			yield return new WaitForSeconds(0.03f);
		}
		StartCoroutine(Cycle(0));
	}

#pragma warning disable 414
	private string TwitchHelpMessage = "'!{0} cycle' to cycle the screens, '!{0} submit ENTRIES' to submit ENTRIES. Keep in mind to replace any 'X'.";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
		command = command.ToLowerInvariant();
		if (command == "cycle")
		{
			for (int i = 0; i < 2; i++)
			{
				Button[0].OnInteract();
				yield return null;
				Button[1].OnInteract();
				yield return new WaitForSeconds(5f);
			}
		}
		else if (command.Split(' ').Length == 2 && command.Split(' ')[1].Length == 7 && command.Split(' ')[0] == "submit")
		{
			command = command.Split(' ')[1];
			string alphabet = "abcdefghijklmnopqrstuvwyz";
			for (int i = 0; i < 7; i++)
			{
				if (!alphabet.Contains(command[i]))
				{
					yield return "sendtochaterror Invalid command.";
					yield break;
				}
			}
			for (int i = 0; i < 7; i++)
			{
				for (int j = 0; j < 25; j++)
					if (command[i] == alphabet[j])
					{
						while (((j - mazepos) % 5 + 5) % 5 > 2)
						{
							Button[4].OnInteract();
							yield return null;
						}
						while (((j - mazepos) % 5 + 5) % 5 > 0)
						{
							Button[5].OnInteract();
							yield return null;
						}
						while ((j / 5 - mazepos / 5 + 5) % 5 > 2)
						{
							Button[2].OnInteract();
							yield return null;
						}
						while ((j / 5 - mazepos / 5 + 5) % 5 > 0)
						{
							Button[3].OnInteract();
							yield return null;
						}
						j = 25;
					}
				Button[6].OnInteract();
				yield return null;
			}
			yield return "solve";
		}
		else
			yield return "sendtochaterror Invalid command.";
		yield return null;
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return true;
		if (!solved)
		{
			string alphabet = "abcdefghijklmnopqrstuvwyz";
			for (int i = 0; i < 7; i++)
			{
				for (int j = 0; j < 25; j++)
					if (words[0].ToLowerInvariant().Replace('x', keyletter)[i] == alphabet[j])
					{
						while (((j - mazepos) % 5 + 5) % 5 > 2)
						{
							Button[4].OnInteract();
							yield return null;
						}
						while (((j - mazepos) % 5 + 5) % 5 > 0)
						{
							Button[5].OnInteract();
							yield return null;
						}
						while ((j / 5 - mazepos / 5 + 5) % 5 > 2)
						{
							Button[2].OnInteract();
							yield return null;
						}
						while ((j / 5 - mazepos / 5 + 5) % 5 > 0)
						{
							Button[3].OnInteract();
							yield return null;
						}
						j = 25;
					}
				Button[6].OnInteract();
				yield return true;
			}
		}
	}
}
