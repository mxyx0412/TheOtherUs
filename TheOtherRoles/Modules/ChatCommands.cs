using System;
using HarmonyLib;
using System.Linq;
using TheOtherRoles.Players;
using TheOtherRoles.Utilities;
using UnityEngine;
using static TheOtherRoles.TheOtherRoles;

namespace TheOtherRoles.Modules {
    [HarmonyPatch]
    public static class ChatCommands {
        public static bool isLover(this PlayerControl player) => !(player == null) && (player == Lovers.lover1 || player == Lovers.lover2);
        public static bool isTeamJackal(this PlayerControl player) => !(player == null) && (player == Jackal.jackal || player == Sidekick.sidekick);

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
        private static class SendChatPatch {
            static bool Prefix(ChatController __instance) {
                string text = __instance.TextArea.text;
                bool handled = false;
                if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) {
                    if (text.ToLower().StartsWith("/kick ")) {
                        string playerName = text.Substring(6);
                        PlayerControl target = CachedPlayer.AllPlayers.FirstOrDefault(x => x.Data.PlayerName.Equals(playerName));
                        if (target != null && AmongUsClient.Instance != null && AmongUsClient.Instance.CanBan()) {
                            var client = AmongUsClient.Instance.GetClient(target.OwnerId);
                            if (client != null) {
                                AmongUsClient.Instance.KickPlayer(client.Id, false);
                                handled = true;
                            }
                        }
                    } else if (text.ToLower().StartsWith("/ban ")) {
                        string playerName = text.Substring(6);
                        PlayerControl target = CachedPlayer.AllPlayers.FirstOrDefault(x => x.Data.PlayerName.Equals(playerName));
                        if (target != null && AmongUsClient.Instance != null && AmongUsClient.Instance.CanBan()) {
                            var client = AmongUsClient.Instance.GetClient(target.OwnerId);
                            if (client != null) {
                                AmongUsClient.Instance.KickPlayer(client.Id, true);
                                handled = true;
                            }
                        }
                    }
                }
                
                if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) {
                    if (text.ToLower().Equals("/murder")) {
                        CachedPlayer.LocalPlayer.PlayerControl.Exiled();
                        FastDestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(CachedPlayer.LocalPlayer.Data, CachedPlayer.LocalPlayer.Data);
                        handled = true;
                    } else if (text.ToLower().StartsWith("/color ")) {
                        handled = true;
                        int col;
                        if (!Int32.TryParse(text.Substring(7), out col)) {
                            __instance.AddChat(CachedPlayer.LocalPlayer.PlayerControl, "Unable to parse color id\nUsage: /color {id}");
                        }
                        col = Math.Clamp(col, 0, Palette.PlayerColors.Length - 1);
                        CachedPlayer.LocalPlayer.PlayerControl.SetColor(col);
                        __instance.AddChat(CachedPlayer.LocalPlayer.PlayerControl, "Changed color succesfully");;
                    } 
                }

                if (text.ToLower().StartsWith("/tp ") && CachedPlayer.LocalPlayer.Data.IsDead) {
                    string playerName = text.Substring(4).ToLower();
                    PlayerControl target = CachedPlayer.AllPlayers.FirstOrDefault(x => x.Data.PlayerName.ToLower().Equals(playerName));
                    if (target != null) {
                        CachedPlayer.LocalPlayer.transform.position = target.transform.position;
                        handled = true;
                    }
                }


                if (text.ToLower().StartsWith("/team") && CachedPlayer.LocalPlayer.PlayerControl.isLover() && CachedPlayer.LocalPlayer.PlayerControl.isTeamCultist())
			{
				if (Cultist.cultist == CachedPlayer.LocalPlayer.PlayerControl)
				{
					Cultist.chatTarget = Helpers.flipBitwise(Cultist.chatTarget);
				}
				if (Follower.follower == CachedPlayer.LocalPlayer.PlayerControl)
				{
					Follower.chatTarget = Helpers.flipBitwise(Follower.chatTarget);
				}
				handled = true;
            
                    
                }

                if (handled) {
                    __instance.TextArea.Clear();
                    __instance.quickChatMenu.ResetGlyphs();
                }
                return !handled;
            }
        }
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public static class EnableChat {
            private static void Postfix(HudManager __instance)
		{
			
				if (!__instance.Chat.isActiveAndEnabled && (CachedPlayer.LocalPlayer.PlayerControl.isLover() || CachedPlayer.LocalPlayer.PlayerControl.isTeamCultist()))
				{
					__instance.Chat.SetVisible(visible: true);
				}
			

                if ((Multitasker.multitasker.FindAll(x => x.PlayerId == CachedPlayer.LocalPlayer.PlayerId).Count > 0) || MapOptionsTor.transparentTasks
         /*   && Jester.jester != CachedPlayer.LocalPlayer.PlayerControl
            && Werewolf.werewolf != CachedPlayer.LocalPlayer.PlayerControl
            && Prosecutor.prosecutor != CachedPlayer.LocalPlayer.PlayerControl
            && Swooper.swooper != CachedPlayer.LocalPlayer.PlayerControl
            && Jackal.jackal != CachedPlayer.LocalPlayer.PlayerControl
            && Sidekick.sidekick != CachedPlayer.LocalPlayer.PlayerControl
            && Arsonist.arsonist != CachedPlayer.LocalPlayer.PlayerControl
            && Amnisiac.amnisiac != CachedPlayer.LocalPlayer.PlayerControl
            && Vulture.vulture != CachedPlayer.LocalPlayer.PlayerControl
            && Lawyer.lawyer != CachedPlayer.LocalPlayer.PlayerControl
            && Pursuer.pursuer != CachedPlayer.LocalPlayer.PlayerControl
            && Thief.thief != CachedPlayer.LocalPlayer.PlayerControl
            */) {
                if (PlayerControl.LocalPlayer.Data.IsDead || PlayerControl.LocalPlayer.Data.Disconnected) return;
                if (!Minigame.Instance) return;

                var Base = Minigame.Instance as MonoBehaviour;
            SpriteRenderer[] rends = Base.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < rends.Length; i++)
            {
                var oldColor1 = rends[i].color[0];
                var oldColor2 = rends[i].color[1];
                var oldColor3 = rends[i].color[2];
                rends[i].color = new Color(oldColor1, oldColor2, oldColor3, 0.5f);
            }
        }
            }
        }

        [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
        public static class SetBubbleName { 
            public static void Postfix(ChatBubble __instance, [HarmonyArgument(0)] string playerName) {
                PlayerControl sourcePlayer = PlayerControl.AllPlayerControls.ToArray().ToList().FirstOrDefault(x => x.Data != null && x.Data.PlayerName.Equals(playerName));
                if (CachedPlayer.LocalPlayer != null && CachedPlayer.LocalPlayer.Data.Role.IsImpostor && (Spy.spy != null && sourcePlayer.PlayerId == Spy.spy.PlayerId || Sidekick.sidekick != null && Sidekick.wasTeamRed && sourcePlayer.PlayerId == Sidekick.sidekick.PlayerId || Jackal.jackal != null && Jackal.wasTeamRed && sourcePlayer.PlayerId == Jackal.jackal.PlayerId) && __instance != null) __instance.NameText.color = Palette.ImpostorRed;
            }
        }

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
        public static class AddChat {
            public static bool Prefix(ChatController __instance, [HarmonyArgument(0)] PlayerControl sourcePlayer)
		{
			PlayerControl playerControl = CachedPlayer.LocalPlayer.PlayerControl;
			bool flag = MeetingHud.Instance != null || LobbyBehaviour.Instance != null || playerControl.Data.IsDead || sourcePlayer.PlayerId == CachedPlayer.LocalPlayer.PlayerId;
			if (__instance != FastDestroyableSingleton<HudManager>.Instance.Chat)
			{
				return true;
			}
			if (playerControl == null)
			{
				return true;
			}
            /* brb
			if (playerControl == Detective.detective)
			{
				return flag;
			}
            */
			if (!playerControl.isTeamCultist() && !playerControl.isLover())
			{
				return flag;
			}
			if (playerControl.isTeamCultist() || playerControl.isLover())
			{
				return sourcePlayer.getChatPartner() == playerControl || playerControl.getChatPartner() == playerControl == (bool)sourcePlayer || flag;
			}
			return flag;
		}
	}

	public static bool isTeamCultist(this PlayerControl player)
	{
		return !(player == null) && (player == Cultist.cultist || player == Follower.follower) && Cultist.cultist != null && Follower.follower != null;
	}
    }
}
