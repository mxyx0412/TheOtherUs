using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using TheOtherRoles.Players;
using TheOtherRoles.Utilities;
using static TheOtherRoles.TheOtherRoles;

namespace TheOtherRoles.Patches {
    [HarmonyPatch(typeof(RoleOptionsData), nameof(RoleOptionsData.GetNumPerGame))]
    class RoleOptionsDataGetNumPerGamePatch{
        public static void Postfix(ref int __result) {
            if (CustomOptionHolder.activateRoles.getBool()) __result = 0; // Deactivate Vanilla Roles if the mod roles are active
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class RoleManagerSelectRolesPatch {
        private static int crewValues;
        private static int impValues;
        public static void Postfix() {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ResetVaribles, Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.resetVariables();

            if (CustomOptionHolder.activateRoles.getBool()) // Don't assign Roles in Tutorial or if deactivated
                assignRoles();
        }

        private static void assignRoles() {
            var data = getRoleAssignmentData();
            assignEnsuredRoles(data); // Assign roles that should always be in the game next
            assignChanceRoles(data); // Assign roles that may or may not be in the game last
            assignRoleTargets(data);
            assignModifiers();
        }

        public static RoleAssignmentData getRoleAssignmentData() {
            // Get the players that we want to assign the roles to. Crewmate and Neutral roles are assigned to natural crewmates. Impostor roles to impostors.
            List<PlayerControl> crewmates = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            crewmates.RemoveAll(x => x.Data.Role.IsImpostor);
            List<PlayerControl> impostors = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            impostors.RemoveAll(x => !x.Data.Role.IsImpostor);

            var crewmateMin = CustomOptionHolder.crewmateRolesCountMin.getSelection();
            var crewmateMax = CustomOptionHolder.crewmateRolesCountMax.getSelection();
            var neutralMin = CustomOptionHolder.neutralRolesCountMin.getSelection();
            var neutralMax = CustomOptionHolder.neutralRolesCountMax.getSelection();
            var impostorMin = CustomOptionHolder.impostorRolesCountMin.getSelection();
            var impostorMax = CustomOptionHolder.impostorRolesCountMax.getSelection();
            
            // Make sure min is less or equal to max
            if (crewmateMin > crewmateMax) crewmateMin = crewmateMax;
            if (neutralMin > neutralMax) neutralMin = neutralMax;
            if (impostorMin > impostorMax) impostorMin = impostorMax;

            // Get the maximum allowed count of each role type based on the minimum and maximum option
            int crewCountSettings = rnd.Next(crewmateMin, crewmateMax + 1);
            int neutralCountSettings = rnd.Next(neutralMin, neutralMax + 1);
            int impCountSettings = rnd.Next(impostorMin, impostorMax + 1);

            // Potentially lower the actual maximum to the assignable players
            int maxCrewmateRoles = Mathf.Min(crewmates.Count, crewCountSettings);
            int maxNeutralRoles = Mathf.Min(crewmates.Count, neutralCountSettings);
            int maxImpostorRoles = Mathf.Min(impostors.Count, impCountSettings);

            // Fill in the lists with the roles that should be assigned to players. Note that the special roles (like Mafia or Lovers) are NOT included in these lists
            Dictionary<byte, int> impSettings = new Dictionary<byte, int>();
            Dictionary<byte, int> neutralSettings = new Dictionary<byte, int>();
            Dictionary<byte, int> crewSettings = new Dictionary<byte, int>();
            
            impSettings.Add((byte)RoleId.Morphling, CustomOptionHolder.morphlingSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Camouflager, CustomOptionHolder.camouflagerSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Vampire, CustomOptionHolder.vampireSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Eraser, CustomOptionHolder.eraserSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Trickster, CustomOptionHolder.tricksterSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Cleaner, CustomOptionHolder.cleanerSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Undertaker, CustomOptionHolder.undertakerSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Miner, CustomOptionHolder.minerSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Warlock, CustomOptionHolder.warlockSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.BountyHunter, CustomOptionHolder.bountyHunterSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Witch, CustomOptionHolder.witchSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Ninja, CustomOptionHolder.ninjaSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Bomber, CustomOptionHolder.bomberSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Blackmailer, CustomOptionHolder.blackmailerSpawnRate.getSelection());

            neutralSettings.Add((byte)RoleId.Jester, CustomOptionHolder.jesterSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Prosecutor, CustomOptionHolder.prosecutorSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Amnisiac, CustomOptionHolder.amnisiacSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Arsonist, CustomOptionHolder.arsonistSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Jackal, CustomOptionHolder.jackalSpawnRate.getSelection());
            // Don't assign Swooper unless Both option is on
            if (!CustomOptionHolder.swooperAsWell.getBool()) 
                neutralSettings.Add((byte)RoleId.Swooper, CustomOptionHolder.swooperSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Werewolf, CustomOptionHolder.werewolfSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Vulture, CustomOptionHolder.vultureSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Lawyer, CustomOptionHolder.lawyerSpawnRate.getSelection());

            crewSettings.Add((byte)RoleId.Mayor, CustomOptionHolder.mayorSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Portalmaker, CustomOptionHolder.portalmakerSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Engineer, CustomOptionHolder.engineerSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Lighter, CustomOptionHolder.lighterSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.BodyGuard, CustomOptionHolder.bodyGuardSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Detective, CustomOptionHolder.detectiveSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.TimeMaster, CustomOptionHolder.timeMasterSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Veteren, CustomOptionHolder.veterenSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Medic, CustomOptionHolder.medicSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Shifter, CustomOptionHolder.shifterSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Swapper,CustomOptionHolder.swapperSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Seer, CustomOptionHolder.seerSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Hacker, CustomOptionHolder.hackerSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Tracker, CustomOptionHolder.trackerSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Snitch, CustomOptionHolder.snitchSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Medium, CustomOptionHolder.mediumSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.NiceGuesser, CustomOptionHolder.guesserSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Sheriff, CustomOptionHolder.sheriffSpawnRate.getSelection()); //adding this here because sheriff role is not being given out
            if (impostors.Count > 1) {
                // Only add Spy if more than 1 impostor as the spy role is otherwise useless
                crewSettings.Add((byte)RoleId.Spy, CustomOptionHolder.spySpawnRate.getSelection());
            }
            crewSettings.Add((byte)RoleId.SecurityGuard, CustomOptionHolder.securityGuardSpawnRate.getSelection());

            return new RoleAssignmentData {
                crewmates = crewmates,
                impostors = impostors,
                crewSettings = crewSettings,
                neutralSettings = neutralSettings,
                impSettings = impSettings,
                maxCrewmateRoles = maxCrewmateRoles,
                maxNeutralRoles = maxNeutralRoles,
                maxImpostorRoles = maxImpostorRoles
            };
        }

        private static void assignSpecialRoles(RoleAssignmentData data) {
            
            // //Assign Cultist
            if (Cultist.isCultistGame) {
                setRoleToRandomPlayer((byte)RoleId.Cultist, data.impostors);
            }
             if (data.impostors.Count >= 2 && data.maxImpostorRoles >= 2 && (rnd.Next(1, 101) <= CustomOptionHolder.cultistSpawnRate.getSelection() * 10))
             {
                 var index = rnd.Next(0, data.impostors.Count);
                 PlayerControl playerControl = data.impostors[index];

                 Helpers.turnToCrewmate(playerControl);
                
                 data.impostors.RemoveAt(index);
                 data.crewmates.Add(playerControl);
                 setRoleToRandomPlayer((byte)RoleId.Cultist, data.impostors);
                 //data.impostors.Count = 1;
                 data.maxImpostorRoles = 1;


             
            }
        }

        private static void selectFactionForFactionIndependentRoles(RoleAssignmentData data) {
            if ((CustomOptionHolder.deputySpawnRate.getSelection() > 0 &&
                CustomOptionHolder.sheriffSpawnRate.getSelection() == 10) ||
                CustomOptionHolder.deputySpawnRate.getSelection() == 0) 
                    data.crewSettings.Add((byte)RoleId.Sheriff, CustomOptionHolder.sheriffSpawnRate.getSelection());


            crewValues = data.crewSettings.Values.ToList().Sum();
            impValues = data.impSettings.Values.ToList().Sum();
        }

        private static void assignEnsuredRoles(RoleAssignmentData data) {
            // Get all roles where the chance to occur is set to 100%
            List<byte> ensuredCrewmateRoles = data.crewSettings.Where(x => x.Value == 10).Select(x => x.Key).ToList();
            List<byte> ensuredNeutralRoles = data.neutralSettings.Where(x => x.Value == 10).Select(x => x.Key).ToList();
            List<byte> ensuredImpostorRoles = data.impSettings.Where(x => x.Value == 10).Select(x => x.Key).ToList();

            // Assign roles until we run out of either players we can assign roles to or run out of roles we can assign to players
            while (
                (data.impostors.Count > 0 && data.maxImpostorRoles > 0 && ensuredImpostorRoles.Count > 0) || 
                (data.crewmates.Count > 0 && (
                    (data.maxCrewmateRoles > 0 && ensuredCrewmateRoles.Count > 0) || 
                    (data.maxNeutralRoles > 0 && ensuredNeutralRoles.Count > 0)
                ))) {
                    
                Dictionary<RoleType, List<byte>> rolesToAssign = new Dictionary<RoleType, List<byte>>();
                if (data.crewmates.Count > 0 && data.maxCrewmateRoles > 0 && ensuredCrewmateRoles.Count > 0) rolesToAssign.Add(RoleType.Crewmate, ensuredCrewmateRoles);
                if (data.crewmates.Count > 0 && data.maxNeutralRoles > 0 && ensuredNeutralRoles.Count > 0) rolesToAssign.Add(RoleType.Neutral, ensuredNeutralRoles);
                if (data.impostors.Count > 0 && data.maxImpostorRoles > 0 && ensuredImpostorRoles.Count > 0) rolesToAssign.Add(RoleType.Impostor, ensuredImpostorRoles);
                
                // Randomly select a pool of roles to assign a role from next (Crewmate role, Neutral role or Impostor role) 
                // then select one of the roles from the selected pool to a player 
                // and remove the role (and any potentially blocked role pairings) from the pool(s)
                var roleType = rolesToAssign.Keys.ElementAt(rnd.Next(0, rolesToAssign.Keys.Count())); 
                var players = roleType == RoleType.Crewmate || roleType == RoleType.Neutral ? data.crewmates : data.impostors;
                var index = rnd.Next(0, rolesToAssign[roleType].Count);
                var roleId = rolesToAssign[roleType][index];
                setRoleToRandomPlayer(rolesToAssign[roleType][index], players);
                rolesToAssign[roleType].RemoveAt(index);

                if (CustomOptionHolder.blockedRolePairings.ContainsKey(roleId)) {
                    foreach(var blockedRoleId in CustomOptionHolder.blockedRolePairings[roleId]) {
                        // Set chance for the blocked roles to 0 for chances less than 100%
                        if (data.impSettings.ContainsKey(blockedRoleId)) data.impSettings[blockedRoleId] = 0;
                        if (data.neutralSettings.ContainsKey(blockedRoleId)) data.neutralSettings[blockedRoleId] = 0;
                        if (data.crewSettings.ContainsKey(blockedRoleId)) data.crewSettings[blockedRoleId] = 0;
                        // Remove blocked roles even if the chance was 100%
                        foreach(var ensuredRolesList in rolesToAssign.Values) {
                            ensuredRolesList.RemoveAll(x => x == blockedRoleId);
                        }
                    }
                }

                // Adjust the role limit
                switch (roleType) {
                    case RoleType.Crewmate: data.maxCrewmateRoles--; crewValues -= 10; break;
                    case RoleType.Neutral: data.maxNeutralRoles--; break;
                    case RoleType.Impostor: data.maxImpostorRoles--; impValues -= 10;  break;
                }
            }
        }

        private static void assignDependentRoles(RoleAssignmentData data) {
            // Roles that prob have a dependent role
            bool sheriffFlag = CustomOptionHolder.deputySpawnRate.getSelection() > 0 
                && CustomOptionHolder.sheriffSpawnRate.getSelection() > 0;

            if (!sheriffFlag) return; // assignDependentRoles is not needed

            int crew = data.crewmates.Count < data.maxCrewmateRoles ? data.crewmates.Count : data.maxCrewmateRoles; // Max number of crew loops
            int imp = data.impostors.Count < data.maxImpostorRoles ? data.impostors.Count : data.maxImpostorRoles; // Max number of imp loops
            int crewSteps = crew / data.crewSettings.Keys.Count(); // Avarage crewvalues deducted after each loop 
            int impSteps = imp / data.impSettings.Keys.Count(); // Avarage impvalues deducted after each loop

            // set to false if needed, otherwise we can skip the loop
            bool isSheriff = !sheriffFlag; 

            // --- Simulate Crew & Imp ticket system ---
            while (crew > 0 && (!isSheriff)) {
                if (!isSheriff && rnd.Next(crewValues) < CustomOptionHolder.sheriffSpawnRate.getSelection()) isSheriff = true;
                crew--;
                crewValues -= crewSteps;
            }

            // --- Assign Main Roles if they won the lottery ---
            if (isSheriff && Sheriff.sheriff == null && data.crewmates.Count > 0 && data.maxCrewmateRoles > 0 && sheriffFlag) { // Set Sheriff cause he won the lottery
                byte sheriff = setRoleToRandomPlayer((byte)RoleId.Sheriff, data.crewmates);
                data.crewmates.ToList().RemoveAll(x => x.PlayerId == sheriff);
                data.maxCrewmateRoles--;
            }

            // --- Assign Dependent Roles if main role exists ---
            if (Sheriff.sheriff != null) { // Deputy
                if (CustomOptionHolder.deputySpawnRate.getSelection() == 10 && data.crewmates.Count > 0 && data.maxCrewmateRoles > 0) { // Force Deputy
                    byte deputy = setRoleToRandomPlayer((byte)RoleId.Deputy, data.crewmates);
                    data.crewmates.ToList().RemoveAll(x => x.PlayerId == deputy);
                    data.maxCrewmateRoles--;
                } else if (CustomOptionHolder.deputySpawnRate.getSelection() < 10) // Dont force, add Deputy to the ticket system
                    data.crewSettings.Add((byte)RoleId.Deputy, CustomOptionHolder.deputySpawnRate.getSelection());
            }
        }
        private static void assignChanceRoles(RoleAssignmentData data) {
            // Get all roles where the chance to occur is set grater than 0% but not 100% and build a ticket pool based on their weight
            List<byte> crewmateTickets = data.crewSettings.Where(x => x.Value > 0 && x.Value < 10).Select(x => Enumerable.Repeat(x.Key, x.Value)).SelectMany(x => x).ToList();
            List<byte> neutralTickets = data.neutralSettings.Where(x => x.Value > 0 && x.Value < 10).Select(x => Enumerable.Repeat(x.Key, x.Value)).SelectMany(x => x).ToList();
            List<byte> impostorTickets = data.impSettings.Where(x => x.Value > 0 && x.Value < 10).Select(x => Enumerable.Repeat(x.Key, x.Value)).SelectMany(x => x).ToList();

            // Assign roles until we run out of either players we can assign roles to or run out of roles we can assign to players
            while (
                (data.impostors.Count > 0 && data.maxImpostorRoles > 0 && impostorTickets.Count > 0) || 
                (data.crewmates.Count > 0 && (
                    (data.maxCrewmateRoles > 0 && crewmateTickets.Count > 0) || 
                    (data.maxNeutralRoles > 0 && neutralTickets.Count > 0)
                ))) {
                
                Dictionary<RoleType, List<byte>> rolesToAssign = new Dictionary<RoleType, List<byte>>();
                if (data.crewmates.Count > 0 && data.maxCrewmateRoles > 0 && crewmateTickets.Count > 0) rolesToAssign.Add(RoleType.Crewmate, crewmateTickets);
                if (data.crewmates.Count > 0 && data.maxNeutralRoles > 0 && neutralTickets.Count > 0) rolesToAssign.Add(RoleType.Neutral, neutralTickets);
                if (data.impostors.Count > 0 && data.maxImpostorRoles > 0 && impostorTickets.Count > 0) rolesToAssign.Add(RoleType.Impostor, impostorTickets);
                
                // Randomly select a pool of role tickets to assign a role from next (Crewmate role, Neutral role or Impostor role) 
                // then select one of the roles from the selected pool to a player 
                // and remove all tickets of this role (and any potentially blocked role pairings) from the pool(s)
                var roleType = rolesToAssign.Keys.ElementAt(rnd.Next(0, rolesToAssign.Keys.Count()));
                var players = roleType == RoleType.Crewmate || roleType == RoleType.Neutral ? data.crewmates : data.impostors;
                var index = rnd.Next(0, rolesToAssign[roleType].Count);
                var roleId = rolesToAssign[roleType][index];
                setRoleToRandomPlayer(roleId, players);
                rolesToAssign[roleType].RemoveAll(x => x == roleId);

                if (CustomOptionHolder.blockedRolePairings.ContainsKey(roleId)) {
                    foreach(var blockedRoleId in CustomOptionHolder.blockedRolePairings[roleId]) {
                        // Remove tickets of blocked roles from all pools
                        crewmateTickets.RemoveAll(x => x == blockedRoleId);
                        neutralTickets.RemoveAll(x => x == blockedRoleId);
                        impostorTickets.RemoveAll(x => x == blockedRoleId);
                    }
                }

                // Adjust the role limit
                switch (roleType) {
                    case RoleType.Crewmate: data.maxCrewmateRoles--; break;
                    case RoleType.Neutral: data.maxNeutralRoles--;break;
                    case RoleType.Impostor: data.maxImpostorRoles--;break;
                }
            }
        }

        private static void assignRoleTargets(RoleAssignmentData data) {
            // Set Lawyer Target
            if (Lawyer.lawyer != null) {
                var possibleTargets = new List<PlayerControl>();
                foreach (PlayerControl p in CachedPlayer.AllPlayers) {
                    if (p.Data.IsDead || p.Data.Disconnected) continue; // Don't assign dead people
                    if (p == Lovers.lover1 || p == Lovers.lover2) continue; // Don't allow a lover target
                    if (p == Jester.jester && !Lawyer.targetCanBeJester) continue;
                    if (p == Lawyer.lawyer) continue; // Dont't assign Self

                    if (p != Jester.jester)
                    {
                        if (Lawyer.targetCanBe == 0 && (p.Data.Role.IsImpostor || !p.Data.Role.IsImpostor))
                            possibleTargets.Add(p);

                        if (Lawyer.targetCanBe == 1 && (p.Data.Role.IsImpostor || p == Jackal.jackal || p == Swooper.swooper || p == Werewolf.werewolf || p == Prosecutor.prosecutor || p == Vulture.vulture || p == Arsonist.arsonist))
                            possibleTargets.Add(p);

                        if (Lawyer.targetCanBe == 2 && !p.Data.Role.IsImpostor && !p == Jackal.jackal && !p == Swooper.swooper && !p == Werewolf.werewolf && !p == Prosecutor.prosecutor && !p == Vulture.vulture && !p == Arsonist.arsonist)
                            possibleTargets.Add(p);
                    }

                    if (Lawyer.targetCanBeJester && p == Jester.jester)
                        possibleTargets.Add(p);

                }
                if (possibleTargets.Count == 0) {
                    MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.LawyerPromotesToPursuer, Hazel.SendOption.Reliable, -1);
                    AmongUsClient.Instance.FinishRpcImmediately(w);
                    RPCProcedure.lawyerPromotesToPursuer();
                } else {
                    var target = possibleTargets[TheOtherRoles.rnd.Next(0, possibleTargets.Count)];
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.LawyerSetTarget, Hazel.SendOption.Reliable, -1);
                    writer.Write(target.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.lawyerSetTarget(target.PlayerId);
                }
            }
			
            // Start Set Prosecutor Target
            if (Prosecutor.prosecutor != null) {
                var possibleTargets = new List<PlayerControl>();
                foreach (PlayerControl p in PlayerControl.AllPlayerControls) {
                    if (p.Data.IsDead || p.Data.Disconnected) continue; // Don't assign dead people
                    if (p == Lovers.lover1 || p == Lovers.lover2) continue; // Don't allow a lover target
                    if (p.Data.Role.IsImpostor ||  p == Jackal.jackal || p == Swooper.swooper) continue; // Dont allow imp / jackal target
					if (p == Spy.spy) continue; // Dont allow Spy to be target
					// I simply don't want these targets, as they can hard counter Prosecutor
					if (p == Mayor.mayor || p == Sheriff.sheriff || p == Swapper.swapper || p == Shifter.shifter) continue;
                    if (RoleInfo.getRoleInfoForPlayer(p).FirstOrDefault().isNeutral) continue; // Don't allow neutral target
                    possibleTargets.Add(p);
                }
                if (possibleTargets.Count == 0) {
                    MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ProsecutorChangesRole, Hazel.SendOption.Reliable, -1);
                    AmongUsClient.Instance.FinishRpcImmediately(w);
                    RPCProcedure.prosecutorChangesRole();
                } else {
                    var target = possibleTargets[TheOtherRoles.rnd.Next(0, possibleTargets.Count)];
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ProsecutorSetTarget, Hazel.SendOption.Reliable, -1);
                    writer.Write(target.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.prosecutorSetTarget(target.PlayerId);
                }
            }
            // End Set Prosecutor Target
        }

        private static void assignModifiers() {
            var modifierMin = CustomOptionHolder.modifiersCountMin.getSelection();
            var modifierMax = CustomOptionHolder.modifiersCountMax.getSelection();
            if (modifierMin > modifierMax) modifierMin = modifierMax;
            int modifierCountSettings = rnd.Next(modifierMin, modifierMax + 1);
            List<PlayerControl> players = PlayerControl.AllPlayerControls.ToArray().ToList();
            List<PlayerControl> impPlayer = new List<PlayerControl>(players);
            List<PlayerControl> crewPlayer = new List<PlayerControl>(players);
            impPlayer.RemoveAll(x => !x.Data.Role.IsImpostor);
            crewPlayer.RemoveAll(x => x.Data.Role.IsImpostor);
            int modifierCount = Mathf.Min(players.Count, modifierCountSettings);

            if (modifierCount == 0) return;

            List<RoleId> globalModifiers = new List<RoleId>();
            List<RoleId> ensuredGlobalModifiers = new List<RoleId>();
            List<RoleId> chanceGlobalModifiers = new List<RoleId>();

            List<RoleId> impModifiers = new List<RoleId>();
            List<RoleId> ensuredImpModifiers = new List<RoleId>();
            List<RoleId> chanceImpModifiers = new List<RoleId>();

            List<RoleId> crewModifiers = new List<RoleId>();
            List<RoleId> ensuredCrewModifiers = new List<RoleId>();
            List<RoleId> chanceCrewModifiers = new List<RoleId>();

            globalModifiers.AddRange(new List<RoleId>
            {
                RoleId.Tiebreaker,
                RoleId.Mini,
                RoleId.Bloody,
                RoleId.AntiTeleport,
                RoleId.Slueth,
                RoleId.Vip,
                RoleId.Invert,
                RoleId.Blind,
                RoleId.Bait
            });

            impModifiers.AddRange(new List<RoleId>
            {
                RoleId.EvilGuesser
            });

            crewModifiers.AddRange(new List<RoleId>
            {
                RoleId.Sunglasses,
                RoleId.Torch,
                RoleId.Indomitable,
                RoleId.Cursed
            });

            if (rnd.Next(1, 101) <= CustomOptionHolder.modifierLover.getSelection() * 10)
            { // Assign lover
                bool isEvilLover = rnd.Next(1, 101) <= CustomOptionHolder.modifierLoverImpLoverRate.getSelection() * 10;
                byte firstLoverId;

                if (isEvilLover) firstLoverId = setModifierToRandomPlayer((byte)RoleId.Lover, impPlayer);
                else firstLoverId = setModifierToRandomPlayer((byte)RoleId.Lover, crewPlayer);
                byte secondLoverId = setModifierToRandomPlayer((byte)RoleId.Lover, crewPlayer, 1);

                players.RemoveAll(x => x.PlayerId == firstLoverId || x.PlayerId == secondLoverId);
                modifierCount--;
            }

            foreach (RoleId m in globalModifiers)
            {
                if (getSelectionForRoleId(m) == 10) ensuredGlobalModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true) / 10));
                else chanceGlobalModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true)));
            }
            foreach (RoleId m in impModifiers)
            {
                if (getSelectionForRoleId(m) == 10) ensuredImpModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true) / 10));
                else chanceImpModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true)));
            }
            foreach (RoleId m in crewModifiers)
            {
                if (getSelectionForRoleId(m) == 10) ensuredCrewModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true) / 10));
                else chanceCrewModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true)));
            }

            assignModifiersToPlayers(ensuredImpModifiers, impPlayer, modifierCount); // Assign ensured imp modifier

            assignModifiersToPlayers(ensuredCrewModifiers, crewPlayer, modifierCount); // Assign ensured crew modifier
            modifierCount -= ensuredCrewModifiers.Count;

            assignModifiersToPlayers(ensuredGlobalModifiers, players, modifierCount); // Assign ensured global modifier
            modifierCount -= ensuredGlobalModifiers.Count;

            if (modifierCount <= 0) return;
            int chanceGlobalModifierCount = Mathf.Min(modifierCount, chanceGlobalModifiers.Count);
            List<RoleId> chanceGlobalModifierToAssign = new List<RoleId>();
            while (chanceGlobalModifierCount > 0 && chanceGlobalModifiers.Count > 0)
            {
                var index = rnd.Next(0, chanceGlobalModifiers.Count);
                RoleId modifierId = chanceGlobalModifiers[index];
                chanceGlobalModifierToAssign.Add(modifierId);

                int modifierSelection = getSelectionForRoleId(modifierId);
                while (modifierSelection > 0)
                {
                    chanceGlobalModifiers.Remove(modifierId);
                    modifierSelection--;
                }
                chanceGlobalModifierCount--;
            }
            assignModifiersToPlayers(chanceGlobalModifierToAssign, players, modifierCount); // Assign chance Global modifier

            int chanceCrewModifierCount = Mathf.Min(modifierCount, chanceCrewModifiers.Count);
            List<RoleId> chanceCrewModifierToAssign = new List<RoleId>();
            while (chanceCrewModifierCount > 0 && chanceCrewModifiers.Count > 0)
            {
                var index = rnd.Next(0, chanceCrewModifiers.Count);
                RoleId modifierId = chanceCrewModifiers[index];
                chanceCrewModifierToAssign.Add(modifierId);

                int modifierSelection = getSelectionForRoleId(modifierId);
                while (modifierSelection > 0)
                {
                    chanceCrewModifiers.Remove(modifierId);
                    modifierSelection--;
                }
                chanceCrewModifierCount--;
            }
            assignModifiersToPlayers(chanceCrewModifierToAssign, crewPlayer, modifierCount); // Assign chance Crew modifier

            int chanceImpModifierCount = Mathf.Min(modifierCount, chanceImpModifiers.Count);
            List<RoleId> chanceImpModifierToAssign = new List<RoleId>();
            while (chanceImpModifierCount > 0 && chanceImpModifiers.Count > 0)
            {
                var index = rnd.Next(0, chanceImpModifiers.Count);
                RoleId modifierId = chanceImpModifiers[index];
                chanceImpModifierToAssign.Add(modifierId);

                int modifierSelection = getSelectionForRoleId(modifierId);
                while (modifierSelection > 0)
                {
                    chanceImpModifiers.Remove(modifierId);
                    modifierSelection--;
                }
                chanceImpModifierCount--;
            }
            assignModifiersToPlayers(chanceImpModifierToAssign, impPlayer, modifierCount); // Assign chance Imp modifier

        }

        private static byte setRoleToRandomPlayer(byte roleId, List<PlayerControl> playerList, bool removePlayer = true) {
            var index = rnd.Next(0, playerList.Count);
            byte playerId = playerList[index].PlayerId;
            if (removePlayer) playerList.RemoveAt(index);

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetRole, Hazel.SendOption.Reliable, -1);
            writer.Write(roleId);
            writer.Write(playerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.setRole(roleId, playerId);

            if (roleId == (byte)RoleId.Jackal && CustomOptionHolder.swooperAsWell.getBool()) {
                if (rnd.Next(1, 101) <= CustomOptionHolder.swooperSpawnRate.getSelection() * 10) {
                    MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetRole, Hazel.SendOption.Reliable, -1);
                    writer2.Write((byte)RoleId.Swooper);
                    writer2.Write(playerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    RPCProcedure.setRole((byte)RoleId.Swooper, playerId);
                }
            }

            return playerId;
        }

        private static byte setModifierToRandomPlayer(byte modifierId, List<PlayerControl> playerList, byte flag = 0) {
            var index = rnd.Next(0, playerList.Count);
            byte playerId = playerList[index].PlayerId;
            playerList.RemoveAt(index);

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetModifier, Hazel.SendOption.Reliable, -1);
            writer.Write(modifierId);
            writer.Write(playerId);
            writer.Write(flag);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.setModifier(modifierId, playerId, flag);
            return playerId;
        }

        private static void assignModifiersToPlayers(List<RoleId> modifiers, List<PlayerControl> playerList, int modifierCount) {
            modifiers = modifiers.OrderBy(x => rnd.Next()).ToList(); // randomize list

            while (modifierCount < modifiers.Count) {
                var index = rnd.Next(0, modifiers.Count);
                modifiers.RemoveAt(index);
            }

            byte playerId;

            if (modifiers.Contains(RoleId.Cursed)) {
                List<PlayerControl> crewPlayer = new List<PlayerControl>(playerList);
                crewPlayer.RemoveAll(x => x.Data.Role.IsImpostor || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral));
                playerId = setModifierToRandomPlayer((byte)RoleId.Cursed, crewPlayer);
                playerList.RemoveAll(x => x.PlayerId == playerId);
                modifiers.RemoveAll(x => x == RoleId.Cursed);
            }


            modifiers.RemoveAll(x => x == RoleId.NiceGuesser);
            modifiers.RemoveAll(x => x == RoleId.EvilGuesser);
			modifiers.RemoveAll(x => x == RoleId.Cursed);



            if (modifiers.Contains(RoleId.Sunglasses)) {
                List<PlayerControl> crewPlayer = new List<PlayerControl>(playerList);
                crewPlayer.RemoveAll(x => x.Data.Role.IsImpostor || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral));
                int sunglassesCount = 0;
                while (sunglassesCount < modifiers.FindAll(x => x == RoleId.Sunglasses).Count) {
                    playerId = setModifierToRandomPlayer((byte)RoleId.Sunglasses, crewPlayer);
                    crewPlayer.RemoveAll(x => x.PlayerId == playerId);
                    playerList.RemoveAll(x => x.PlayerId == playerId);
                    sunglassesCount++;
                }
                modifiers.RemoveAll(x => x == RoleId.Sunglasses);
            }
            if (modifiers.Contains(RoleId.Torch)) {
                List<PlayerControl> crewPlayer = new List<PlayerControl>(playerList);
                crewPlayer.RemoveAll(x => x.Data.Role.IsImpostor || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral));
                int torchCount = 0;
                while (torchCount < modifiers.FindAll(x => x == RoleId.Torch).Count) {
                    playerId = setModifierToRandomPlayer((byte)RoleId.Torch, crewPlayer);
                    crewPlayer.RemoveAll(x => x.PlayerId == playerId);
                    playerList.RemoveAll(x => x.PlayerId == playerId);
                    torchCount++;
                }
                modifiers.RemoveAll(x => x == RoleId.Torch);
            }

            foreach (RoleId modifier in modifiers) {
                if (playerList.Count == 0) break;
                playerId = setModifierToRandomPlayer((byte)modifier, playerList);
                playerList.RemoveAll(x => x.PlayerId == playerId);
            }
        }

        private static int getSelectionForRoleId(RoleId roleId, bool multiplyQuantity = false) {
            int selection = 0;
            switch (roleId) {
                case RoleId.Lover:
                    selection = CustomOptionHolder.modifierLover.getSelection(); break;
                case RoleId.Tiebreaker:
                    selection = CustomOptionHolder.modifierTieBreaker.getSelection(); break;
                case RoleId.Indomitable:
                    selection = CustomOptionHolder.modifierIndomitable.getSelection(); break;
                case RoleId.Cursed:
                    selection = CustomOptionHolder.modifierCursed.getSelection(); break;
                case RoleId.Slueth:
                    selection = CustomOptionHolder.modifierSlueth.getSelection(); break;
                case RoleId.Blind:
                    selection = CustomOptionHolder.modifierBlind.getSelection(); break;
                case RoleId.Mini:
                    selection = CustomOptionHolder.modifierMini.getSelection(); break;
                case RoleId.Bait:
                    selection = CustomOptionHolder.modifierBait.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierBaitQuantity.getQuantity();
                    break;
                case RoleId.Bloody:
                    selection = CustomOptionHolder.modifierBloody.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierBloodyQuantity.getQuantity();
                    break;
                case RoleId.AntiTeleport:
                    selection = CustomOptionHolder.modifierAntiTeleport.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierAntiTeleportQuantity.getQuantity();
                    break;
                case RoleId.Sunglasses:
                    selection = CustomOptionHolder.modifierSunglasses.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierSunglassesQuantity.getQuantity();
                    break;
                case RoleId.Torch:
                    selection = CustomOptionHolder.modifierTorch.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierTorchQuantity.getQuantity();
                    break;
                case RoleId.Vip:
                    selection = CustomOptionHolder.modifierVip.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierVipQuantity.getQuantity();
                    break;
                case RoleId.Invert:
                    selection = CustomOptionHolder.modifierInvert.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierInvertQuantity.getQuantity();
                    break;
                case RoleId.EvilGuesser:
                    selection = CustomOptionHolder.modifierAssassin.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierAssassinQuantity.getQuantity();
                    break;
            }
                 
            return selection;
        }


        public class RoleAssignmentData {
            public List<PlayerControl> crewmates {get;set;}
            public List<PlayerControl> impostors {get;set;}
            public Dictionary<byte, int> impSettings = new Dictionary<byte, int>();
            public Dictionary<byte, int> neutralSettings = new Dictionary<byte, int>();
            public Dictionary<byte, int> crewSettings = new Dictionary<byte, int>();
            public int maxCrewmateRoles {get;set;}
            public int maxNeutralRoles {get;set;}
            public int maxImpostorRoles {get;set;}
        }
        
        private enum RoleType {
            Crewmate = 0,
            Neutral = 1,
            Impostor = 2
        }

    }
}

