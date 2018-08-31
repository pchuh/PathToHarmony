﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace Cutscenes {
	public class Cutscene : MonoBehaviour {

		private readonly float TRANSITION_DELAY_TIME = 0.35f;

		private static Sprite[] backgrounds;
		private CutsceneCharacter[] characters;
		private CutsceneCharacter leftCharacter;
		private CutsceneCharacter rightCharacter;
		private CutsceneScript script;
		public bool inProgress;

		[SerializeField]
		public Image currentBackground;
		[SerializeField]
		public Image leftImage;
		[SerializeField]
		public Image rightImage;
		[SerializeField]
		public Image dialogueBackground;
		[SerializeField]
		public Text dialogueText;

		void Awake() {
			backgrounds = new Sprite[] {
			Resources.Load<Sprite>("Sprites/EmptyBackground"),
			Resources.Load<Sprite>("Sprites/TempBackground")
		};
		}

		//Called by the level writer. This is my attempt to both let the user assign refrences in the inspector
		// (which i'm pretty sure means we can't use a factory) and avoid the "can we beat the first frame"
		// race condition. I hope. I think. Godddd unity :( 

		public void setup(CutsceneCharacter[] characters, CutsceneScript script, Cutscene refrenceDupe = null) {
			this.characters = characters;
			this.script = script;

			if (refrenceDupe != null) {
				this.currentBackground = refrenceDupe.currentBackground;
				this.leftImage = refrenceDupe.leftImage;
				this.rightImage = refrenceDupe.rightImage;
				this.dialogueBackground = refrenceDupe.dialogueBackground;
				this.dialogueText = refrenceDupe.dialogueText;
			}
		}

		// Use this for initialization
		public IEnumerator Start() {
			inProgress = true;
			setUIVisibility(true);
			yield return PlayScene();
			setUIVisibility(false);

			this.inProgress = false;
		}

		//TODO: If someone wants to reimplement this with some sort of coutroutine lambda hack and get rid of
		// the ugly ugly PlayScene and the CutsceneAction type I'll love you forever. Maybe smth like:
		// https://answers.unity.com/questions/542115/is-there-any-way-to-use-coroutines-with-anonymous.html
		IEnumerator PlayScene() {
			foreach (CutsceneScriptLine line in script.script) {
				switch (line.action) {
					case CutsceneAction.TransitionIn:
						yield return transitionIn(line.character, line.side);
						break;
					case CutsceneAction.TransitionOut:
						yield return transitionOut(line.side);
						break;
					case CutsceneAction.SetCharacter:
						yield return setCharacter(line.character, line.side);
						break;
					case CutsceneAction.FocusSide:
						focusSide(line.side);
						break;
					case CutsceneAction.SetBackground:
						setBackground(line.background);
						break;
					case CutsceneAction.SayDialogue:
						yield return sayDialogue(line.character, line.dialogue);
						break;
					default:
						Debug.LogError("Unrecognized CutsceneAction type");
						break;
				}

				yield return new WaitForSeconds(0.5f);
			}
		}

		public IEnumerator sayDialogue(CutsceneCharacter character, string dialogue) {
			focusSide(character);
			dialogue = character.name.ToUpper() + ": " + dialogue;
			dialogueText.text = dialogue;
			yield return new WaitForSeconds(dialogue.Length * 0.04f + 1.5f);
		}

		public void setBackground(CutsceneBackground background) {
			currentBackground.sprite = backgrounds[(int)(background)];
		}

		public void focusSide(CutsceneCharacter character) {
			CutsceneSide side = (character == leftCharacter) ? CutsceneSide.Left : CutsceneSide.Right;
			focusSide(side);
		}

		public void focusSide(CutsceneSide side) {
			if (side == CutsceneSide.Left) {
				restoreColor(leftImage);
				if (rightImage != null) {
					greyOut(rightImage);
				}
			} else if (side == CutsceneSide.Right) {
				restoreColor(rightImage);
				if (leftImage != null) {
					greyOut(leftImage);
				}
			}
		}

		public IEnumerator setCharacter(CutsceneCharacter character, CutsceneSide side) {
			bool isLeft = (side == CutsceneSide.Left);
			CutsceneCharacter oldCharacter = isLeft ? leftCharacter : rightCharacter;
			if (oldCharacter != null) {
				yield return transitionOut(side);
				yield return new WaitForSeconds(0.5f);
			}

			if (isLeft) {
				leftCharacter = character;
			} else {
				rightCharacter = character;
			}
			yield return transitionIn(character, side);
		}

		public IEnumerator transitionOut(CutsceneSide side) {
			Image img;
			CutsceneCharacter oldCharacter;
			string animationName;
			bool isLeft = side == CutsceneSide.Left;
			if (isLeft) {
				img = leftImage;
				oldCharacter = leftCharacter;
				animationName = "CutsceneLeftCharacterOut";
			} else {
				img = rightImage;
				oldCharacter = rightCharacter;
				animationName = "CutsceneRightCharacterOut";
			}

			Animation anim = img.GetComponent<Animation>();
			anim.Play(animationName);
			yield return WaitForAnimation(anim);

			if (isLeft) {
				// leftImage = null;
				leftCharacter = null;
			} else {
				// rightImage = null;
				rightCharacter = null;
			}
		}

		public IEnumerator transitionIn(CutsceneCharacter character, CutsceneSide side) {
			Image img;
			CutsceneCharacter oldCharacter;
			string animationName;
			if (side == CutsceneSide.Left) {
				img = leftImage;
				oldCharacter = leftCharacter;
				animationName = "CutsceneLeftCharacterIn";
			} else {
				img = rightImage;
				oldCharacter = rightCharacter;
				animationName = "CutsceneRightCharacterIn";
			}

			img.sprite = character.currentExpression;
			Animation anim = img.GetComponent<Animation>();
			anim.Play(animationName);
			yield return WaitForAnimation(anim);
		}

		private IEnumerator WaitForAnimation(Animation animation) {
			do {
				yield return null;
			} while (animation.isPlaying);
		}

		public void restoreColor(Image img) {
			img.color = Color.white;
		}

		public void greyOut(Image img) {
			img.color = new Color(0.5f, 0.5f, 0.5f);
		}

		private void setUIVisibility(bool visible) {
			currentBackground.enabled = visible;
			dialogueBackground.enabled = visible;
			dialogueText.enabled = visible;

			if (leftImage != null) {
				leftImage.enabled = visible;
			}
			if (rightImage != null) {
				rightImage.enabled = visible;
			}
		}

	}
}