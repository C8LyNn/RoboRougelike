/* Catalog is static and audited; values never used as HTML. */
(function(root,factory){'use strict';const c=factory();if(typeof module==='object'&&module.exports)module.exports=c;else root.B01Catalog=c;})(typeof globalThis!=='undefined'?globalThis:this,function(){'use strict';return Object.freeze({
version:1,
pl:{
 'app.title':'Laboratorium lokalizacji interfejsu',
 'app.subtitle':'Garaż i walka — podgląd tekstu bez zapisu w grze.',
 'nav.garage':'Garaż', 'nav.combat':'Walka', 'nav.current':'Ekran: {phase}',
 'control.language':'Język interfejsu', 'control.quantity':'Liczba części',
 'control.motion':'Tryb podglądu', 'control.button':'Przeładuj podgląd',
 'garage.title':'Projekt pojazdu', 'garage.scrap':'Dostępny złom: {scrap}',
 'garage.parts':{$plural:'count',one:'{count} część',few:'{count} części',many:'{count} części',other:'{count} części'},
 'garage.mount':'Zamontuj {module}', 'garage.warning':'Nie można rozpocząć: brakuje napędu.',
 'combat.title':'Kontrola uzbrojenia', 'combat.weapon':'Aktywna broń: {weapon}',
 'combat.reload':'Przeładowanie: {key}', 'combat.ammo':{$plural:'count',one:'{count} pocisk',few:'{count} pociski',many:'{count} pocisków',other:'{count} pocisków'},
 'status.ready':'Gotowy', 'status.blocked':'Zablokowany',
 'notice.changed':'Zmieniono język; stan gry bez zmian.',
 'notice.pseudo':'Pseudolokalizacja: celowo wydłużone etykiety.',
 'audit.title':'Weryfikacja długości etykiet', 'audit.long':'Najdłuższa etykieta w bieżącym widoku: {count} znaków',
 'audit.overflow':'Przewijanie poziome: {phase}', 'audit.format':'Katalog zgodny: {count} kluczy',
 'key.move':'Ruch', 'key.fire':'Strzał', 'part.wheel':'Koło', 'weapon.railgun':'Działo szynowe'
},
en:{
 'app.title':'Interface localization lab',
 'app.subtitle':'Garage and combat — text preview without saving to game.',
 'nav.garage':'Garage','nav.combat':'Combat','nav.current':'Screen: {phase}',
 'control.language':'Interface language', 'control.quantity':'Part count',
 'control.motion':'Preview mode','control.button':'Refresh preview',
 'garage.title':'Vehicle blueprint','garage.scrap':'Available scrap: {scrap}',
 'garage.parts':{$plural:'count',one:'{count} part',few:'{count} parts',many:'{count} parts',other:'{count} parts'},
 'garage.mount':'Mount {module}', 'garage.warning':'Cannot start: drive module missing.',
 'combat.title':'Weapon control', 'combat.weapon':'Active weapon: {weapon}',
 'combat.reload':'Reload: {key}', 'combat.ammo':{$plural:'count',one:'{count} projectile',few:'{count} projectiles',many:'{count} projectiles',other:'{count} projectiles'},
 'status.ready':'Ready', 'status.blocked':'Blocked',
 'notice.changed':'Language changed; game state is unchanged.',
 'notice.pseudo':'Pseudo-localization: labels intentionally lengthened.',
 'audit.title':'Label length audit','audit.long':'Longest label in current view: {count} characters',
 'audit.overflow':'Horizontal scrolling: {phase}','audit.format':'Catalog valid: {count} keys',
 'key.move':'Movement','key.fire':'Fire', 'part.wheel':'Wheel', 'weapon.railgun':'Railgun'
}
});});
