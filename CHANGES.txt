Strategia 1.10.0
- Support replacing headImage (static images) for admin department.
- Better dynamic resizing based on count of departments.
- Removed some old Reflection that is no longer needed.
- Fixed an NRE when no avatar is specified in a department config.

Strategia 1.9.0
- Rebuild for KSP 1.10.0
- Fix from TonkaCrash for issue where strategies for improving vessel values applied the improved value to different parts of the same type.
- Fixed a minor issue where the Strategy notifier could report incorrect information.

Strategia 1.8.0
- Rebuild for KSP 1.8.0
- Minor typo correction (thanks Galenmacil).

Strategia 1.7.4
- Check hasSolidSurface flag for gas giants (thanks LucasHazelwood).
- Added title to Strategia agent to prevent warnings in log (thanks denyasis).

Strategia 1.7.3
- Rebuild for KSP 1.5.x.
- Fixed active strategy text in facility right-click menu (thanks avalancha).

Strategia 1.7.2
- Fixed logic for determining strategy level - this was causing some strategies to be the same for all levels (thanks ibanix).
- Fixed Astronaut Training Strategies not actually costing extra (thanks Jukelo).

Strategia 1.7.1
- Fixed some issues with the kerbal portraits not refreshing properly when the level is dynamically changed (thanks zwinst).
- Fixed error handling when tech tree can't be loaded (thanks zwinst).
- Fixed the astronaut training strategy that was broken in 1.7.0 (thanks MistaJunior).
- Fixed ugly number format in some places.

Strategia 1.7.0
- Rebuild for KSP 1.4.1.
- Fixed issue where the level boosting strategies could boost a level too high and break things (thanks Mihara).
- Removed stock references to slider values in messages for strategies that weren't available due to insufficient Admin Building level.

Strategia 1.6.0
- Rebuild for KSP 1.3.0.
- Celestial body programs now increase the likelihood of receiving contracts related to the celestial body in question.
- Fixed issue where removal of contract decline penalty on contract slot machine was permanent.

Strategia 1.5.0
- Massive Scale Launches strategy now incrementally gives the bonuses (you can still get the level 1 bonus if you don't meet the threshold for level 3).
- Show the correct max number of strategies on the description text of the Administration building.
- Minor Operations department balance (thanks Stratickus).
- Reduce Kerbal recovery reputation by a factor of 10 (thanks BureauJaeger).
- Fix issue with Stagnated Research not being selectable (thanks Stratickus).
- Adjust order of Gene/Wernher in the admin building.
- Use correct currency symbols.

Strategia 1.4.0
- Rebuild for KSP 1.2.x.

Strategia 1.3.0
- Added basic support for ResearchBodies (strategies for unresearched bodies are unavailable).
- Combine popups triggered with the same text/purpose.
- Made contracts work better with Contract Configurator 1.15.x.
- Added loading tip.
- Fixed To Boldly Go not awarding bonuses when the science reward slider isn't set to 100% (thanks Smu).
- Fixed minor issues with currency popups.

Strategia 1.2.4
- No longer lose reputation bonuses when upgrading/downgrading within the Free Ice Cream I/II/III strategies (thanks ibanix).
- Bonuses/maluses are preserved when upgrading or downgrading Free Ice Cream.
- Don't change active contracts when activating Free Ice Cream for the first time (thanks ZachPruckowski).
- Increased Moho rewards for various strategies (thanks ibanix).
- Fixed mission requirement in moon probe strategies (thanks ibanix).

Strategia 1.2.3
- Corrected minimum Contract Configurator version checking.
- Fixed moon probe strategies to not be mutually exclusive (thanks westamastaflash).
- CurrencyOperationByContract now looks at child groups as well.
- Fixed broken Custom Barn Kit check.
- Allow splashed or landed for probe contracts (thanks Norcalplanner).
- Moon probe strategies stop being offered once the moon in quest is orbited, not just reached.

Strategia 1.2.2
- Output the adjusted number of max strategies allowed in the admin building so that people stop posting about it in the thread (thanks literally everyone).
- Fixed exceptions when researching a tech (thanks smjjames).
- Change currency popups symbols to work around font issue.
- Fixed exception loading Massive Scale Launches (thanks KocLobster).
- Workaround for KSP 1.1 bug where vertical scrollbars don't work in Admin UI.
- Fixed some issues with cancelling contract-based strategies.
- Allow a Kerbal returning being landed on a moon to trigger the planetary strategies to handle cases where the ship doesn't make it home (thanks dlrk).
- Fixed Pilot Focus III ISP adjustments with multi-mode engines (thanks lude).

Strategia 1.2.1
- Fixed Pilot/Engineer/Scientist Focus strategies not actually giving the stated contract bonuses (thanks DeathProphet).
- Fixed compatibility with Sigma Binary (thanks sentania).
- Fixed hint text for crewed/uncrewed missions (thanks severedsolo).
- Fixed planetary probe strategies so they can't be re-done (thanks ibanix).
- Fixed reputation/funds being lost on scene change - normally after the quicksave but breaks KRASH (thanks garwel and linuxgurugamer).

Strategia 1.2.0
- Support for KSP 1.1
- Fixed additional issue with incorrect ISP assignment in Pilot Focus III (thanks TorgHacker).
- Increased the cache size for the contract slot machine strategy.

Strategia 1.1.1
- Fixed issue with reputation being awarded when it's not supposed to (thanks Wercho).
- Make crewed mission requirement a little bit clearer (thanks Zoidos).
- Make moon uncrewed missions only require an orbit of the homeworld (thanks a_shack).
- Fixed issue with FlyBy mission caused by Contract Configurator 1.9.8 (thanks smjjames).

Strategia 1.1.0
- Mun/Minmus Program can each be completed in turn.
- Created a logo for the contract agent.
- Changed Media Circus I bonus to prevent infinite reputation exploit.
- Added support for Sigma Binary.
- Fixed impactor contracts for some configurations (thanks smjjames).
- Fixed bug that allowed more than the max number of strategies to be active if the last one was a mission strategy (thanks Death Engineering).
- Fixed issue where Massive Scale Launch bonuses would apply when going from landed => orbit on *any* body (thanks smjjames).
- Fixed Astronaut Training exception when hiring two crew members in quick succession (thanks smjjames).
- Fixed strategies that change vessel values (like Pilot Focus III) to check for more events - like crew transfers (thanks smjjames).
- Force set ISP when changing it in Pilot Focus III (thanks smjjames).
- Fixed possible issue with multipliers when switching between Pilot/Engineer focuses.
- Fixed issue where Contract Slot Machine values kep getting re-rolled (thanks smjjames).
- Fixed issue where Free Ice Cream time-counter would reset each time a save is loaded.

Strategia 1.0.0
- Initial release.
