using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using PlanarCipherWords;
using Rnd = UnityEngine.Random;
public class mechanusCipherScript : MonoBehaviour {

	public KMAudio Audio;
	public AudioClip[] Sounds;
	public KMSelectable[] Gears;
	public KMSelectable[] Button;
	public GameObject[] Eyelids;
	public GameObject Pupil;
	public TextMesh[] Text;
	public KMBombModule Module;

	private bool solved;
	private bool[] isanimating = new bool[3];
	private float[] rotations = { -90f, -90f, -90f, -90f, -90f, -90f, -90f, -90f, -90f, -90f, -90f, -90f, -90f, -90f };
	private float[] gearrotations = { -45f / 13f, 270 + 45f / 13f };
	private string[] words = new string[3];
	private string[] displays1 = new string[3];
	private string[] displays2 = new string[3];
	private int[] wordindex = new int[2];
	private bool[] backside = new bool[2];
	private string encrypted;
	private string binaries;
	private string swaps = "";
	private int gearpos;
	private int remaining;
	private int done;
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
				if (pos < 14)
					StartCoroutine(TotalFlip(pos));
				else
				{
					string alphabet = "abcdefghijklmnopqrstuvwxyz";
					if (input.Length < 7 && !isanimating[0])
					{
						if (input.Length != 7)
							input += alphabet[gearpos];
						StartCoroutine(FlipPanel(input.Length - 1, true));
					}
				}
			}
			return false;
		};
	}

	private KMSelectable.OnInteractHandler GearPressed(int pos)
	{
		return delegate
		{
			Gears[pos].AddInteractionPunch();
			if (!solved)
			{
				if (pos == 0)
					StartCoroutine(TurnGears(false));
				else
					StartCoroutine(TurnGears(true));
			}
			return false;
		};
	}

	void Awake () {
		_moduleID = _moduleIdCounter++;

		for (int i = 0; i < Button.Length; i++)
			Button[i].OnInteract += PanelPressed(i);
		for (int i = 0; i < Gears.Length; i++)
			Gears[i].OnInteract += GearPressed(i);
		for (int i = 0; i < words.Length; i++)
			words[i] = Wordlist.wordlist[Rnd.Range(0, Wordlist.wordlist.Length)];
		for (int i = 0; i < 7; i++)
		{
			swaps += Rnd.Range(1, 8).ToString();
			string x = swaps[i * 2].ToString();
			while (x == swaps[i * 2].ToString())
				x = Rnd.Range(1, 8).ToString();
			swaps += x;
		}
		MechanusEncrypt(words[0], words[1], words[2], swaps);
		string swaps2 = "";
		for (int i = 0; i < 7; i++)
			swaps2 += swaps[i * 2];
		for (int i = 0; i < 7; i++)
			swaps2 += swaps[i * 2 + 1];
		swaps = swaps2;
		displays1 = new string[] { encrypted, words[1], binaries };
		displays2 = new string[] { swaps.Substring(0, 7), swaps.Substring(7, 7), words[2] };
		for (int i = 0; i < 7; i++)
			Text[i * 2 + 26].text = displays1[0][i].ToString().ToUpperInvariant();
		for (int i = 0; i < 7; i++)
			Text[i * 2 + 40].text = displays2[0][i].ToString().ToUpperInvariant();
		Text[55].text = keyletter.ToString().ToUpperInvariant();
	}

	private void MechanusEncrypt(string word, string keyword, string keyword2, string swappos)
	{
		word = word.ToLowerInvariant();
		keyword = keyword.ToLowerInvariant();
		keyword2 = keyword2.ToLowerInvariant();
		Debug.LogFormat("[Mechanus Cipher #{0}] Encrypted word is {1}.", _moduleID, word.ToUpperInvariant());
		string alphabet = "abcdefghijklmnopqrstuvwxyz";
		string key = "";
		string encryption = "";
		int j = 0;

		//keyword to key
		for (int i = 0; i < 26; i++)
		{
			int k = 0;
			for (int l = 0; l < 26; l++)
				if (alphabet[l] == keyword[j])
					k = l;
			while (key.Contains(alphabet[k]))
			{
				k++; k %= 26;
			}
			key += alphabet[k];
			j++; j %= 7;
		}
		Debug.LogFormat("[Mechanus Cipher #{0}] Key made from {1} is {2}.", _moduleID, keyword.ToUpperInvariant(), key.ToUpperInvariant());

		//step 3
		for (int i = 0; i < 7; i++)
		{
			int[] k = { 0, 0 };
			for (int l = 0; l < 26; l++)
			{
				if (key[l] == keyword2[i])
					k[0] = l;
				if (key[l] == word[i])
					k[1] = l;
			}
			j = 1;
			int p = 0;
			for (int l = 0; l < 3; l++)
			{
				p += ((6 - ((k[0] / j) % 3) - ((k[1] / j) % 3)) % 3) * j;
				j *= 3;
			}
			if (p == 26)
				p = k[1];
			encryption += key[p];
		}
		Debug.LogFormat("[Mechanus Cipher #{0}] Encryption of step 3 using keyword {1} results in {2}.", _moduleID, keyword2.ToUpperInvariant(), encryption.ToUpperInvariant());

		//step 2
		word = encryption;
		encryption = "";
		int[,] binary = new int[4,7];
		int x = Rnd.Range(0, 16);
		for (int i = 0; i < 26; i++)
			if (word[6] == key[i])
				j = i;
		int fact = 8;
		for (int i = 0; i < 4; i++)
		{
			binary[i, 6] = (x / fact) % 2;
			fact /= 2;
		}
		for (int i = 5; i >= 0; i--)
		{
			int safe = 0;
			while (word[i] != key[j] || safe == 0)
			{
				j = (j + 1) % 26;
				x = (x + 15) % 16;
				safe++;
			}
			fact = 8;
			for (int n = 0; n < 4; n++)
			{
				binary[n, i] = (x / fact) % 2;
				fact /= 2;
			}
			if (safe > 16)
				binaries = '1' + binaries;
			else
				binaries = '0' + binaries;
		}
		if (Rnd.Range(0, 2) == 1)
		{
			j = (j + 16) % 26;
			binaries = '1' + binaries;
		}
		else
			binaries = '0' + binaries;
		for (int i = 6; i >= 0; i--)
		{
			int c = 8 * binary[(i * 4) / 7, (i * 4) % 7] + 4 * binary[((i * 4) + 1) / 7, ((i * 4) + 1) % 7] + 2 * binary[((i * 4) + 2) / 7, ((i * 4) + 2) % 7] + binary[((i * 4) + 3) / 7, ((i * 4) + 3) % 7];
			int safe = 0;
			while (x != c || safe == 0)
			{
				j = (j + 1) % 26;
				x = (x + 15) % 16;
				safe++;
			}
			if (Rnd.Range(0, 2) == 1 && safe <= 10 && i != 6)
				j = (j + 16) % 26;
			encryption = key[j] + encryption;
		}
		int safe2 = 0;
		while (x != 0 || safe2 == 0)
		{
			j = (j + 1) % 26;
			x = (x + 15) % 16;
			safe2++;
		}
		if (Rnd.Range(0, 2) == 1 && safe2 <= 10)
			j = (j + 16) % 26;
		keyletter = key[j];
		string binarylog = "";
		for (int i = 0; i < 28; i++)
			binarylog += binary[i / 7, i % 7].ToString();
		Debug.Log(binarylog);
		Debug.LogFormat("[Mechanus Cipher #{0}] Encryption of step 2 results in binary string {1}, key letter {2}, and encrypted word {3}.", _moduleID, binaries, keyletter.ToString().ToUpperInvariant(), encryption.ToUpperInvariant());

		//step 1
		for (int i = swappos.Length / 2 - 1; i >= 0; i--)
		{
			word = encryption;
			encryption = "";
			int a = int.Parse(swappos[i * 2].ToString()) - 1;
			int b = int.Parse(swappos[i * 2 + 1].ToString()) - 1;
			for (int l = 0; l < (new int[] { a, b }).Min(); l++)
				encryption += word[l];
			for (int l = (new int[] { a, b }).Max(); l >= (new int[] { a, b }).Min(); l--)
				encryption += word[l];
			for (int l = (new int[] { a, b }).Max() + 1; l < 7; l++)
				encryption += word[l];
			encrypted = encryption;
		}
		Debug.LogFormat("[Mechanus Cipher #{0}] Encryption of step 1 using swapping key {1} (interpret as pairs) results in {2}.", _moduleID, swappos, encrypted.ToUpperInvariant());
	}

	private IEnumerator Shutdown()
	{
		float[] eyerots = { 90f, 90f };
		input = "       ";
		displays1 = new string[] { "       ", "       ", "       " };
		displays2 = new string[] { "       ", "       ", "       " };
		StartCoroutine(TotalFlip(0));
		yield return new WaitForSeconds(0.06f);
		StartCoroutine(TotalFlip(7));
		yield return new WaitForSeconds(0.06f);
		for (int i = 0; i < 18; i++)
		{
			eyerots[0] += 5f;
			eyerots[1] -= 5f;
			for (int j = 0; j < 2; j++)
				Eyelids[j].transform.localEulerAngles = new Vector3(eyerots[j], 0f, 0f);
			yield return new WaitForSeconds(0.03f);
		}
		Module.HandlePass();
	}

	private IEnumerator Ouch()
	{
		float eyescale = 0.5f;
		for (int i = 0; i < 5; i++)
		{
			eyescale -= 0.075f;
			Pupil.transform.localScale = new Vector3(eyescale, 0.2f, eyescale);
			yield return new WaitForSeconds(0.03f);
		}
		for (int i = 0; i < 20; i++)
		{
			eyescale += 0.01875f;
			Pupil.transform.localScale = new Vector3(eyescale, 0.2f, eyescale);
			yield return new WaitForSeconds(0.03f);
		}
	}

	private IEnumerator FlipPanel(int pos, bool sub)
	{
		Audio.PlaySoundAtTransform("Ping", Module.transform);
		if (pos < 7)
		{
			if (!sub)
			{
				if (backside[0])
					Text[pos * 2 + 27].text = displays1[wordindex[0]][pos].ToString().ToUpperInvariant();
				else
					Text[pos * 2 + 26].text = displays1[wordindex[0]][pos].ToString().ToUpperInvariant();
			}
			else
			{
				if (!backside[0])
					Text[pos * 2 + 27].text = input[pos].ToString().ToUpperInvariant();
				else
					Text[pos * 2 + 26].text = input[pos].ToString().ToUpperInvariant();
			}
		}
		else
		{
			if (backside[1])
				Text[pos * 2 + 27].text = displays2[wordindex[1]][pos - 7].ToString().ToUpperInvariant();
			else
				Text[pos * 2 + 26].text = displays2[wordindex[1]][pos - 7].ToString().ToUpperInvariant();
		}
		for (int i = 0; i < 18; i++)
		{
			rotations[pos] += 10f;
			Button[pos].transform.localEulerAngles = new Vector3(rotations[pos], 0f, 0f);
			yield return new WaitForSeconds(0.03f);
		}
		if (pos == 6 && sub && input.Length == 7 && !solved)
		{
			if (input.ToUpperInvariant() == words[0].ToUpperInvariant())
			{
				solved = true;
				StartCoroutine(Shutdown());
			}
			else
			{
				Debug.LogFormat("[Mechanus Cipher #{0}] Submitted {1}, but I expected {2}.", _moduleID, input.ToUpperInvariant(), words[0].ToUpperInvariant());
				Module.HandleStrike();
				StartCoroutine(TotalFlip(0));
				StartCoroutine(Ouch());
			}
		}
		Audio.PlaySoundAtTransform("Ping", Module.transform);
	}

	private IEnumerator TurnGears(bool forward)
	{
		if (remaining == done)
			Audio.PlaySoundAtTransform("GearClink", Module.transform);
		if (forward)
		{
			remaining++;
			gearpos = (gearpos + 1) % 26;
		}
		else
		{
			gearpos = (gearpos + 25) % 26;
			remaining--;
		}
		for (int i = 0; i < 26; i++)
		{
			if (i == gearpos)
				Text[i].color = new Color32(255, 255, 0, 255);
			else
				Text[i].color = new Color32(63, 35, 0, 255);
		}
		while (isanimating[2])
			yield return null;
		if (forward && remaining > done || !forward && remaining < done)
		{
			if (forward)
				done++;
			else
				done--;
			isanimating[2] = true;
			for (int i = 0; i < 9; i++)
			{
				if (forward)
				{
					gearrotations[0] -= (360f / 13f) / 18f;
					gearrotations[1] += (360f / 13f) / 18f;
				}
				else
				{
					gearrotations[0] += (360f / 13f) / 18f;
					gearrotations[1] -= (360f / 13f) / 18f;
				}
				for (int j = 0; j < 2; j++)
					Gears[j].transform.localEulerAngles = new Vector3(-90f, gearrotations[j], 0f);
				yield return new WaitForSeconds(0.03f);
			}
			isanimating[2] = false;
			if (!(forward && remaining > done || !forward && remaining < done))
				Audio.PlaySoundAtTransform("GearClink", Module.transform);
		}
	}

	private IEnumerator TotalFlip(int pos)
	{
		if (pos < 7)
		{
			while (isanimating[0])
				yield return null;
			isanimating[0] = true;
			wordindex[0] = (wordindex[0] + 1) % 3;
			backside[0] = !backside[0];
			if (input != "" && pos < 7)
			{
				int inplen = input.Length;
				input = "       ";
				for (int i = 0; i < 7; i++)
				{ 
					for (int j = 0; j < 7; j++)
						if ((j - i == pos || j + i == pos) && j < inplen)
							StartCoroutine(FlipPanel(j, true));
					yield return new WaitForSeconds(0.06f);
				}
				input = "";
				wordindex[0] = 0;
				yield return new WaitForSeconds(0.6f);
			}
			if (!solved)
			{
				for (int i = 0; i < 7; i++)
				{
					for (int j = 0; j < 7; j++)
						if (j - i == pos || j + i == pos)
							StartCoroutine(FlipPanel(j, false));
					yield return new WaitForSeconds(0.06f);
				}
				yield return new WaitForSeconds(0.6f);
			}
			isanimating[0] = false;
		}
		else if (pos < 14)
		{
			while (isanimating[1])
				yield return null;
			isanimating[1] = true;
			backside[1] = !backside[1];
			wordindex[1] = (wordindex[1] + 1) % 3;
			for (int i = 0; i < 7; i++)
			{
				for (int j = 7; j < 14; j++)
					if (j - i == pos || j + i == pos)
						StartCoroutine(FlipPanel(j, false));
				yield return new WaitForSeconds(0.06f);
			}
			yield return new WaitForSeconds(0.6f);
			isanimating[1] = false;
		}
	}

#pragma warning disable 414
	private string TwitchHelpMessage = "'!{0} cycle' to cycle the screens, '!{0} toggle [1,2,both]' to toggle screens, '!{0} submit ENTRIES' to submit ENTRIES.";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
		command = command.ToLowerInvariant();

		switch (command)
		{
			case "toggle 1":
				Button[0].OnInteract();
				yield return null;
				break;

			case "toggle 2":
				Button[1].OnInteract();
				yield return null;
				break;

			case "toggle both":
				Button[0].OnInteract();
				yield return null;
				Button[1].OnInteract();
				yield return null;
				break;

			case "cycle":
				for (int i = 0; i < 2; i++)
				{
					Button[0].OnInteract();
					yield return null;
					Button[1].OnInteract();
					yield return new WaitForSeconds(5f);
				}
				break;
        }

        if (command.Split(' ').Length == 2 && command.Split(' ')[1].Length == 7 && command.Split(' ')[0] == "submit")
		{
			command = command.Split(' ')[1];
			string alphabet = "abcdefghijklmnopqrstuvwxyz";
			for (int i = 0; i < 7; i++)
				if (!alphabet.Contains(command[i]))
				{
					yield return "sendtochaterror Invalid command.";
					yield break;
				}
			for (int i = 0; i < 7; i++)
			{
				for (int j = 0; j < 26; j++)
					if (command[i] == alphabet[(gearpos + j) % 26])
					{
						if (j > 13)
							while (alphabet[gearpos] != command[i])
							{
								Gears[0].OnInteract();
								yield return null;
							}
						else
							while (alphabet[gearpos] != command[i])
							{
								Gears[1].OnInteract();
								yield return null;
							}
						j = 26;
					}
				Button[14].OnInteract();
				yield return null;
			}
			yield return "solve";
		}
		yield return null;
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return true;
		if (!solved)
		{
			string alphabet = "abcdefghijklmnopqrstuvwxyz";
			for (int i = 0; i < 7; i++)
			{
				for (int j = 0; j < 26; j++)
					if (words[0].ToLowerInvariant()[i] == alphabet[(gearpos + j) % 26])
					{
						if (j > 13)
							while (alphabet[gearpos] != words[0].ToLowerInvariant()[i])
							{
								Gears[0].OnInteract();
								yield return null;
							}
						else
							while (alphabet[gearpos] != words[0].ToLowerInvariant()[i])
							{
								Gears[1].OnInteract();
								yield return null;
							}
						j = 26;
					}
				Button[14].OnInteract();
				yield return true;
			}
		}
	}
}
