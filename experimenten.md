# Opgave 1 — Wereldkaart: Analyse, Experimentatie & Resultaten

**Student:** Ismail Aras  
**Vak:** Applied Programming  
**Academiejaar:** 2025–2026  

---

## 1. Algemene beschrijving van het project

Voor Opgave 1 werd een interactieve wereldkaart ontwikkeld in **C# met WPF**.

De applicatie ondersteunt:

- het inlezen en tonen van GeoJSON;
- pannen en zoomen zonder GIS- of kaartbibliotheek;
- polygon reduction met Ramer-Douglas-Peucker en Visvalingam-Whyatt;
- drie verschillende kaartprojecties;
- longitude- en latitudeweergave onder de muis;
- een vogelvluchtroute via SLERP;
- OpenStreetMap-steden;
- zoomafhankelijke stadslabels;
- spatial indexing via een R-tree;
- interactieve vergelijking tussen originele en vereenvoudigde polygonen.

De applicatie gebruikt voor de kaartweergave hoofdzakelijk:

- `Canvas`;
- `ScaleTransform`;
- `TranslateTransform`;
- `PathGeometry`.

Er wordt geen GIS-library gebruikt voor het projecteren, pannen of zoomen van de kaart.

---

## 2. Architectuur

De solution werd opgesplitst in vier projecten:

```text
WorldMap.App
WorldMap.Core
WorldMap.Infrastructure
WorldMap.PolygonReducer
```

### 2.1 WorldMap.Core

`WorldMap.Core` bevat de domeinmodellen, interfaces en algoritmes die niet rechtstreeks afhankelijk zijn van WPF of concrete databronnen.

Voorbeelden:

- `Country`;
- `City`;
- `GeoCoordinate`;
- `ProjectedPoint`;
- `MapDataset`;
- `IMapProjection`;
- `ICountryRepository`;
- `ICityRepository`;
- `ICitySpatialIndex`;
- `IPolygonReducer`;
- SLERP-logica;
- city-prioritering.

Hierdoor blijft de centrale logica gescheiden van de gebruikersinterface.

---

### 2.2 WorldMap.Infrastructure

`WorldMap.Infrastructure` bevat concrete implementaties voor externe gegevens en infrastructuur.

Voorbeelden:

- GeoJSON uitlezen;
- OSM uitlezen met OsmSharp;
- implementatie van de spatial index;
- repositories voor landen en steden.

De `App` kent voornamelijk de interfaces en krijgt concrete implementaties via Dependency Injection.

---

### 2.3 WorldMap.App

`WorldMap.App` is de WPF-gebruikersinterface.

Dit project bevat onder andere:

- `MainWindow`;
- het `MainWindowViewModel`;
- de kaartweergave;
- muisinteractie;
- pan en zoom;
- routeweergave;
- city labels;
- dataset- en projectiekeuze.

---

### 2.4 WorldMap.PolygonReducer

Voor polygon reduction werd een aparte Console-applicatie ontwikkeld.

Deze applicatie:

1. leest het originele GeoJSON-bestand;
2. voert RDP uit;
3. voert VW uit;
4. schrijft nieuwe GeoJSON-bestanden;
5. toont statistieken over punten, bestandsgrootte en rekentijd.

---

## 3. Dependency Injection

Om concrete implementaties niet rechtstreeks in `MainWindow` aan te maken, wordt Dependency Injection gebruikt.

Voorbeeld van constructor injection:

```csharp
public MainWindow(
    ICountryRepository countryRepository,
    ICityRepository cityRepository,
    ICitySpatialIndex citySpatialIndex,
    MainWindowViewModel viewModel)
{
    ...
}
```

De concrete implementaties worden centraal geregistreerd.

Voorbeeld:

```csharp
services.AddSingleton<
    ICountryRepository,
    GeoJsonCountryRepository>();

services.AddSingleton<
    ICityRepository,
    OsmCityRepository>();

services.AddSingleton<
    ICitySpatialIndex,
    CitySpatialIndex>();
```

Hierdoor is `MainWindow` niet rechtstreeks afhankelijk van bijvoorbeeld `GeoJsonCountryRepository` of `OsmCityRepository`.

Dit zorgt voor:

- lagere koppeling;
- betere testbaarheid;
- duidelijke verantwoordelijkheden;
- eenvoudigere vervanging van implementaties.

---

## 4. MVVM

De applicatie gebruikt het MVVM-principe.

Het `MainWindowViewModel` bevat UI-state zoals:

- geselecteerde projectie;
- geselecteerde dataset;
- huidige zoom;
- coördinatentekst;
- aantal landen;
- aantal zichtbare steden;
- rendertijd;
- laadtijd;
- informatie over de geselecteerde reduction-dataset.

De WPF-interface gebruikt databinding om deze gegevens weer te geven.

Voorbeeld:

```xml
<TextBlock Text="{Binding CoordinateText}" />
```

en:

```xml
<ComboBox
    ItemsSource="{Binding Projections}"
    SelectedItem="{Binding SelectedProjection}"
    DisplayMemberPath="Name" />
```

De sterk visuele logica, zoals het tekenen op een `Canvas`, blijft gedeeltelijk in de View omdat deze logica rechtstreeks afhankelijk is van WPF.

---

# 5. GeoJSON en landsgrenzen

Als bronbestand voor de wereldkaart wordt de Natural Earth Admin 0 Countries dataset gebruikt.

Bestand:

```text
ne_50m_admin_0_countries.geojson
```

De GeoJSON-reader ondersteunt:

- `Polygon`;
- `MultiPolygon`;
- buitenringen;
- binnenringen / holes.

In totaal worden ongeveer **241 landen/features** ingelezen.

De polygonen worden vervolgens omgezet naar WPF `PathGeometry`-objecten.

De landsgrenzen worden opgevuld weergegeven zodat de landen duidelijk zichtbaar zijn.

---

## 5.1 Vereenvoudigde dataset bij opstart

De applicatie start standaard met:

```text
RDP 0.020
```

en niet met de originele dataset.

Hierdoor is meteen bij het opstarten een vereenvoudigde versie van de landsgrenzen zichtbaar.

De gebruiker kan daarna interactief schakelen tussen:

- Original;
- RDP 0.020;
- VW 0.050.

---

## 5.2 Screenshot

> **Screenshot toevoegen:** volledige wereldkaart bij opstart met `RDP 0.020`.

---

# 6. Pan en Zoom

Pannen en zoomen werd volledig zelf geïmplementeerd zonder GIS- of map-library.

Voor de kaart worden twee WPF-transformaties gecombineerd:

```text
ScaleTransform
TranslateTransform
```

De `ScaleTransform` bepaalt de zoomfactor.

De `TranslateTransform` bepaalt de verschuiving van de kaart.

---

## 6.1 Zoomen rond de muispositie

Tijdens het zoomen wordt eerst bepaald welke positie op de kaart zich onder de muis bevindt.

Na het aanpassen van de schaal wordt de translatie gecorrigeerd zodat hetzelfde geografische gebied ongeveer onder de muis blijft staan.

Dit maakt het zoomen veel natuurlijker dan zoomen rond het midden van het scherm.

---

## 6.2 Zoomgrenzen

De ingestelde zoomgrenzen zijn:

```text
Minimum zoom = 1x
Maximum zoom = 20x
```

Bij het minimale zoomniveau wordt de panpositie gereset.

---

## 6.3 Constante grensdikte

Een probleem bij een gewone `ScaleTransform` is dat ook de lijndikte wordt opgeschaald.

Bijvoorbeeld:

```text
StrokeThickness = 0.6
Zoom = 20x

0.6 × 20 = 12 pixels
```

Hierdoor werden landsgrenzen bij sterke zoom te dik.

Dit werd opgelost door de effectieve `StrokeThickness` afhankelijk te maken van de zoom:

```text
StrokeThickness = basisdikte / zoom
```

Daardoor blijft de visuele lijndikte op het scherm ongeveer constant.

---

# 7. Polygon Reduction — Experimentatie

Het originele GeoJSON-bestand bevat:

**99.566 punten**

Een groot aantal punten heeft invloed op:

- bestandsgrootte;
- verwerkingstijd;
- projectieberekeningen;
- WPF-geometrie;
- rendering.

Daarom werden twee reduction-algoritmen onderzocht:

1. Ramer-Douglas-Peucker;
2. Visvalingam-Whyatt.

Voor beide algoritmen wordt **NetTopologySuite** gebruikt.

---

## 7.1 Ramer-Douglas-Peucker

Ramer-Douglas-Peucker, afgekort RDP, vereenvoudigt een lijn door punten te verwijderen die relatief weinig bijdragen aan de vorm.

Het algoritme bekijkt onder andere de afstand van tussenliggende punten ten opzichte van een lijnsegment.

Een hogere tolerantie veroorzaakt een sterkere vereenvoudiging.

---

### 7.1.1 Resultaten RDP

| Tolerantie | Punten vóór | Punten na | Reductie | Tijd | Bestand na |
|---:|---:|---:|---:|---:|---:|
| 0.001 | 99.566 | 95.638 | 3,95% | 496 ms | 3,72 MB |
| 0.005 | 99.566 | 88.914 | 10,70% | 215 ms | 3,48 MB |
| 0.010 | 99.566 | 71.948 | 27,74% | 178 ms | 2,87 MB |
| **0.020** | **99.566** | **51.459** | **48,32%** | **197 ms** | **2,14 MB** |
| 0.050 | 99.566 | 29.781 | 70,09% | 186 ms | 1,36 MB |

---

### 7.1.2 Analyse RDP

RDP kan het aantal punten zeer sterk verminderen.

Bij een tolerantie van `0.020` wordt bijna de helft van de punten verwijderd:

```text
99.566 → 51.459 punten
```

Dit is een reductie van:

```text
48,32%
```

De bestandsgrootte vermindert daarbij van ongeveer:

```text
3,86 MB → 2,14 MB
```

Visueel blijft de wereldkaart op normale zoom nog zeer sterk lijken op het origineel.

Bij hogere toleranties worden kustlijnen en grenzen duidelijk hoekiger.

---

# 8. Visvalingam-Whyatt

Visvalingam-Whyatt, afgekort VW, gebruikt een andere strategie.

Voor ieder tussenpunt wordt gekeken naar de oppervlakte van de driehoek die gevormd wordt door:

- het vorige punt;
- het huidige punt;
- het volgende punt.

Punten waarvan deze effectieve oppervlakte klein is, zijn minder belangrijk voor de globale vorm.

---

## 8.1 Resultaten VW

| Parameter | Punten vóór | Punten na | Reductie | Tijd | Bestand na |
|---:|---:|---:|---:|---:|---:|
| 0.020 | 99.566 | 85.816 | 13,81% | 340 ms | 3,37 MB |
| **0.050** | **99.566** | **53.271** | **46,50%** | **181 ms** | **2,20 MB** |
| 0.100 | 99.566 | 30.389 | 69,48% | 191 ms | 1,38 MB |
| 0.200 | 99.566 | 15.169 | 84,76% | 210 ms | 0,83 MB |
| 0.500 | 99.566 | 5.592 | 94,38% | 142 ms | 0,48 MB |
| 1.000 | 99.566 | 2.600 | 97,39% | 115 ms | 0,38 MB |

---

## 8.2 Analyse VW

Voor een vergelijkbare reduction als RDP 0.020 werd gekozen voor:

```text
VW 0.050
```

Resultaat:

```text
99.566 → 53.271 punten
```

Dit is een reductie van ongeveer:

```text
46,50%
```

De bestandsgrootte daalt naar ongeveer:

```text
2,20 MB
```

VW behoudt bij vergelijkbare reductie vaak iets meer van het natuurlijke verloop van de grens.

---

# 9. Vergelijking RDP, VW en geen reduction

Om een eerlijke vergelijking mogelijk te maken, werden RDP en VW gekozen met ongeveer dezelfde reductiegraad.

| Dataset | Punten | Reductie | Bestand |
|---|---:|---:|---:|
| Original | 99.566 | 0% | 3,86 MB |
| RDP 0.020 | 51.459 | 48,32% | 2,14 MB |
| VW 0.050 | 53.271 | 46,50% | 2,20 MB |

RDP 0.020 en VW 0.050 bevatten dus ongeveer evenveel punten.

Dit maakt het mogelijk het visuele resultaat eerlijker te vergelijken.

---

## 9.1 Visuele vergelijking

Op wereldniveau is het verschil tussen Original, RDP 0.020 en VW 0.050 relatief klein.

Dit is een belangrijk resultaat.

Ongeveer de helft van de punten kan verwijderd worden zonder dat de kaart op normale schaal drastisch verandert.

Bij sterk inzoomen worden verschillen beter zichtbaar.

### RDP

RDP:

- verwijdert punten agressief;
- maakt lijnen iets hoekiger;
- levert zeer goede reduction op.

### VW

VW:

- behoudt natuurlijke vormen vaak iets beter;
- geeft bij vergelijkbare reduction soms een vloeiender resultaat.

---

## 9.2 Screenshots

> **Screenshot 1:** Original op een gekozen kustgebied.

> **Screenshot 2:** RDP 0.020 op exact dezelfde locatie en zoom.

> **Screenshot 3:** VW 0.050 op exact dezelfde locatie en zoom.

---

# 10. Interactief tonen van polygon reduction

De gebruiker kan in de WPF-applicatie tijdens runtime tussen de drie datasets schakelen.

De ComboBox bevat:

```text
Original
RDP 0.020
VW 0.050
```

Naast de geselecteerde dataset worden ook statistieken getoond.

Bijvoorbeeld voor VW:

```text
Punten: 53.271 | Reductie: 46,50% | Bestand: 2,20 MB
```

Hierdoor kan tijdens de demonstratie onmiddellijk gezien worden hoeveel geometriepunten verwijderd zijn.

---

# 11. Invloed van reduction op performantie

Minder punten betekent dat minder coördinaten verwerkt moeten worden.

Voor ieder punt moet de applicatie potentieel:

1. longitude en latitude lezen;
2. de kaartprojectie toepassen;
3. een WPF-punt genereren;
4. het punt aan een geometrie toevoegen;
5. de geometrie renderen.

Daarom is het logisch dat polygon reduction invloed kan hebben op:

- laadtijd;
- rendertijd;
- geheugenverbruik.

De WPF-applicatie toont daarom actuele load- en rendertijden.

---

## 11.1 Aanvullend performance-experiment

Voor een definitieve vergelijking kunnen Original, RDP en VW meerdere keren worden geladen.

| Dataset | Load run 1 | Load run 2 | Load run 3 | Gemiddeld |
|---|---:|---:|---:|---:|
| Original | ... ms | ... ms | ... ms | ... ms |
| RDP 0.020 | ... ms | ... ms | ... ms | ... ms |
| VW 0.050 | ... ms | ... ms | ... ms | ... ms |

En bijvoorbeeld voor rendering:

| Dataset | Render run 1 | Render run 2 | Render run 3 | Gemiddeld |
|---|---:|---:|---:|---:|
| Original | ... ms | ... ms | ... ms | ... ms |
| RDP 0.020 | ... ms | ... ms | ... ms | ... ms |
| VW 0.050 | ... ms | ... ms | ... ms | ... ms |

De verwachting is dat de gereduceerde datasets minder werk vragen doordat ongeveer de helft van de punten niet meer verwerkt hoeft te worden.

---

# 12. Kaartprojecties

De applicatie bevat drie kaartprojecties:

1. Web Mercator;
2. Belgian Lambert 2008;
3. Azimuthal Equidistant.

Deze projecties werden zelf geïmplementeerd.

Er wordt dus geen projectiebibliotheek gebruikt.

Via een ComboBox kan de gebruiker tijdens runtime van projectie wisselen.

---

# 13. Web Mercator

Web Mercator is een cilindrische conforme kaartprojectie.

De basisformules zijn:

\[
x = R\lambda
\]

en:

\[
y = R \ln\left(\tan\left(\frac{\pi}{4} + \frac{\varphi}{2}\right)\right)
\]

waarbij:

- \(R\) de straal van de aarde voorstelt;
- \(\lambda\) longitude in radialen is;
- \(\varphi\) latitude in radialen is.

---

## 13.1 Eigenschappen

Web Mercator is conform.

Dit betekent dat lokale hoeken redelijk goed behouden blijven.

De projectie vervormt echter oppervlaktes sterk richting de polen.

Een klassiek voorbeeld is Groenland, dat op een Mercatorkaart veel groter lijkt in verhouding tot gebieden rond de evenaar dan in werkelijkheid.

---

## 13.2 Latitude-beperking

Mercator heeft een singulariteit aan de polen.

Daarom wordt de latitude begrensd op ongeveer:

```text
±85,05112878°
```

Hierdoor blijft de projectie numeriek stabiel.

---

## 13.3 Screenshot

> **Screenshot toevoegen:** volledige wereldkaart in Web Mercator.

---

# 14. Belgian Lambert 2008

De tweede projectie is **Belgian Lambert 2008**.

Deze projectie is gebaseerd op Lambert Conformal Conic en is ontworpen voor nauwkeurige geografische weergave van België.

De gebruikte referentie is:

```text
EPSG:3812
```

---

## 14.1 Gebruikte parameters

Belangrijke parameters zijn:

```text
Ellipsoïde: GRS80

Latitude of origin:
50.797815°

Central meridian:
4.35921583333333°

Standard parallel 1:
49.8333333333333°

Standard parallel 2:
51.1666666666667°

False Easting:
649328 m

False Northing:
665262 m
```

---

## 14.2 Eigenschappen

Lambert 2008 is conform.

Hierdoor worden hoeken lokaal goed behouden.

De projectie is specifiek geoptimaliseerd voor België en is daarom niet bedoeld als wereldprojectie.

Wanneer de volledige wereld ermee wordt geprojecteerd, ontstaat buiten België sterke vervorming.

Dat is een eigenschap van de gekozen projectie en geen fout in het projectiealgoritme.

---

## 14.3 Controle met bekende locaties

De implementatie werd getest met verschillende Belgische steden.

| Stad | Longitude | Latitude | X | Y |
|---|---:|---:|---:|---:|
| Brussel | 4,3517 | 50,8503 | 648.798,736 | 671.100,414 |
| Gent | 3,7174 | 51,0543 | 604.328,129 | 693.988,518 |
| Antwerpen | 4,4025 | 51,2194 | 652.352,129 | 712.162,334 |

De inverse berekening werd eveneens getest.

Na:

```text
longitude/latitude
→ Lambert X/Y
→ longitude/latitude
```

werden de oorspronkelijke coördinaten teruggevonden.

---

## 14.4 Screenshot

> **Screenshot toevoegen:** Lambert 2008, ingezoomd rond België.

---

# 15. Azimuthal Equidistant

De derde projectie is de **Azimuthal Equidistant Projection**.

Deze is fundamenteel anders dan Mercator en Lambert.

De kaart wordt geprojecteerd rondom een centraal punt.

In de applicatie ligt dit projectiecentrum rond Brussel.

---

## 15.1 Eigenschappen

Een belangrijke eigenschap van de Azimuthal Equidistant-projectie is dat afstanden en richtingen vanuit het centrum op een bijzondere manier worden behouden.

De projectie is daardoor bijvoorbeeld bruikbaar om afstanden vanuit één gekozen locatie te visualiseren.

De vervorming neemt toe naarmate men verder van het centrum verwijderd is.

Rond het antipodale punt ontstaat een singulariteit.

De renderer houdt rekening met grote sprongen rond dit punt om ongewenste lijnen door de volledige kaart te vermijden.

---

## 15.2 Screenshot

> **Screenshot toevoegen:** volledige Azimuthal Equidistant wereldkaart.

---

# 16. Correctheid van longitude en latitude

De applicatie toont in de statusinformatie de longitude en latitude van de huidige muispositie.

De berekening verloopt als volgt:

```text
Muispositie
↓
Schermpositie
↓
Positie op MapCanvas
↓
Geprojecteerde X/Y
↓
Inverse kaartprojectie
↓
Longitude / Latitude
```

De longitude wordt genormaliseerd naar:

```text
-180° tot +180°
```

Latitudes buiten het geldige bereik worden niet weergegeven.

---

## 16.1 Controlepunten

Bij het bewegen van de muis boven België worden waarden gevonden rond:

```text
Longitude: ongeveer 4°
Latitude: ongeveer 50° à 51°
```

Dit werd gecontroleerd voor alle drie de projecties.

Daarnaast werden forward- en inverseberekeningen getest met bekende coördinaten.

Vooral de Lambert 2008-testen met Brussel, Gent en Antwerpen geven een duidelijke controle van de correctheid.

---

# 17. Vogelvluchtroute

De gebruiker kan twee punten op de kaart aanklikken.

Tussen deze twee punten wordt vervolgens een vogelvluchtroute getekend.

Dit is geen rechte lijn in kaartcoördinaten.

De route wordt berekend als een **grotecirkelroute**, ook orthodroom genoemd.

---

# 18. SLERP

Voor de routeberekening wordt **Spherical Linear Interpolation** gebruikt.

De twee geografische punten worden eerst omgezet naar driedimensionale vectoren op de eenheidsbol.

Voor een geografisch punt geldt:

\[
x = \cos(\varphi)\cos(\lambda)
\]

\[
y = \cos(\varphi)\sin(\lambda)
\]

\[
z = \sin(\varphi)
\]

Daarna wordt tussen beide vectoren geïnterpoleerd.

De SLERP-formule is:

\[
P(t) =
\frac{\sin((1-t)\theta)}{\sin(\theta)}P_0
+
\frac{\sin(t\theta)}{\sin(\theta)}P_1
\]

waarbij:

- \(P_0\) het startpunt is;
- \(P_1\) het eindpunt is;
- \(\theta\) de hoek tussen beide vectoren is;
- \(t\) loopt van 0 tot 1.

De gegenereerde 3D-punten worden daarna terug omgezet naar longitude en latitude.

Pas daarna wordt de geselecteerde kaartprojectie toegepast.

---

# 19. Projectie-onafhankelijkheid van de route

Een belangrijke eigenschap van deze implementatie is dat de routeberekening volledig losstaat van de kaartprojectie.

De stappen zijn:

```text
Start lon/lat
+
Eind lon/lat
↓
SLERP op de aardbol
↓
Reeks lon/lat punten
↓
Gekozen kaartprojectie
↓
Tekenen
```

Hierdoor wordt geografisch steeds dezelfde route gebruikt.

Alleen de tweedimensionale vorm op het scherm verandert.

---

# 20. Route-experiment

Een lange test werd uitgevoerd met:

### Honolulu

```text
Longitude: -157,8583°
Latitude:   21,3069°
```

### Nairobi

```text
Longitude: 36,8219°
Latitude:  -1,2921°
```

Voor de route werden ongeveer:

```text
200 interpolatiepunten
```

gebruikt.

De test controleerde onder andere:

- startpunt;
- eindpunt;
- geldige tussenpunten;
- correcte longitude/latitude.

De test slaagde.

---

## 20.1 Web Mercator

Op Web Mercator is de route duidelijk gebogen.

De route kruist de antimeridiaan.

De wereldkaart heeft daar een kaartnaad bij:

```text
-180° / +180°
```

Daarom wordt de route op die plaats visueel opgesplitst.

---

## 20.2 Lambert 2008

Dezelfde SLERP-route wordt ook voor Lambert gebruikt.

Omdat Lambert 2008 voor België ontworpen is, is de wereldweergave sterk vervormd.

De geografische routepunten blijven echter dezelfde.

---

## 20.3 Azimuthal Equidistant

Op de Azimuthal Equidistant-projectie wordt dezelfde geografische route opnieuw geprojecteerd.

De route verschijnt hier als een andere curve dan in Mercator.

---

## 20.4 Conclusie route-experiment

Dezelfde SLERP-punten worden gebruikt voor alle drie de kaartprojecties.

Hierdoor werd aangetoond dat geografisch dezelfde vogelvluchtroute gevolgd wordt.

De verschillende schermvormen zijn het gevolg van de eigenschappen van de kaartprojectie.

---

## 20.5 Screenshots

> **Screenshot toevoegen:** Honolulu → Nairobi in Web Mercator.

> **Screenshot toevoegen:** Honolulu → Nairobi in Lambert 2008.

> **Screenshot toevoegen:** Honolulu → Nairobi in Azimuthal Equidistant.

---

# 21. OpenStreetMap-steden

Voor de steden wordt gebruikgemaakt van OpenStreetMap.

De ruwe data wordt bewaard in:

```text
cities.osm
```

Voor het uitlezen wordt **OsmSharp** gebruikt.

De OSM-data bevat ongeveer:

```text
2.453 city/town nodes
```

Tijdens analyse werd vastgesteld dat ongeveer:

```text
1.932 nodes een population-tag bevatten
26 nodes een capital-tag bevatten
```

---

# 22. Overpass-query

De gebruikte Overpass-query is:

```text
[out:xml][timeout:120];
(
  node["place"="city"](48,-1,54,9);
  node["place"="town"](48,-1,54,9);
);
out body;
```

De bounding box is:

```text
South = 48
West  = -1
North = 54
East  = 9
```

Hierdoor wordt een relatief grote West-Europese regio opgehaald.

Deze bevat onder andere:

- België;
- Nederland;
- noordelijk Frankrijk;
- westelijk Duitsland;
- een deel van het Verenigd Koninkrijk.

---

# 23. Geselecteerde OSM-tags

De belangrijkste gebruikte OSM-tags zijn:

| Tag | Betekenis | Gebruik |
|---|---|---|
| `name` | naam van de plaats | label op kaart |
| `place=city` | stad | hoge prioriteit |
| `place=town` | kleinere stad | zichtbaar bij hogere zoom |
| `population` | bevolking | prioritering |
| `capital` | bestuurlijke hoofdstad | extra prioriteit |

---

## 23.1 Waarom city en town?

Alleen grote steden tonen zou te weinig informatie geven bij sterke zoom.

Daarom worden zowel:

```text
place=city
```

als:

```text
place=town
```

ingelezen.

`village` werd bewust niet meegenomen omdat dit voor de huidige geografische zone zeer veel extra labels zou veroorzaken.

---

# 24. Zoomafhankelijke city filtering

Niet alle steden worden tegelijk weergegeven.

Afhankelijk van de zoom worden strengere of soepelere criteria toegepast.

De huidige strategie is ongeveer:

| Zoom | Filter |
|---:|---|
| < 1,5x | hoofdstad of population ≥ 500.000 |
| < 2,5x | hoofdstad of population ≥ 100.000 |
| < 4x | hoofdstad of population ≥ 50.000 |
| < 7x | `place=city` of population ≥ 20.000 |
| ≥ 7x | ook kleinere towns |

Hierdoor verschijnt geleidelijk meer detail wanneer de gebruiker inzoomt.

---

# 25. Maximum aantal labels

Naast filtering wordt ook een maximumaantal labels per zoomniveau gebruikt.

Voorbeeld:

| Zoom | Maximum labels |
|---:|---:|
| < 1,5x | 12 |
| < 2,5x | 25 |
| < 4x | 40 |
| < 7x | 60 |
| < 12x | 90 |
| ≥ 12x | 140 |

Hierdoor wordt voorkomen dat de kaart volledig gevuld raakt met tekst.

---

# 26. City Priority

Kandidaten worden gerangschikt op prioriteit.

De prioriteit wordt beïnvloed door:

- hoofdstadstatus;
- `place=city`;
- `place=town`;
- population.

Een belangrijke hoofdstad krijgt bijvoorbeeld een hogere prioriteit dan een kleine town.

Hierdoor worden de belangrijkste labels eerst geplaatst.

---

# 27. Label Collision Detection

Een belangrijk probleem bij stadslabels is overlap.

Daarom wordt voor ieder label eerst de benodigde WPF-grootte gemeten.

Vervolgens wordt een rechthoek rond het label gemaakt.

Voor ieder nieuw label wordt gecontroleerd of deze rechthoek overlapt met een reeds geplaatst label.

Conceptueel:

```text
nieuw label
↓
bereken bounding rectangle
↓
overlap met bestaande labels?
    ↓
ja  → niet tekenen
nee → tekenen
```

Er wordt daarnaast extra padding gebruikt zodat labels niet te dicht tegen elkaar staan.

Hierdoor overlappen stadsnamen vrijwel nooit.

---

# 28. City Overlay

De stadslabels staan niet rechtstreeks op dezelfde geschaalde kaartlaag als de landen.

Er wordt een aparte `CityOverlay` gebruikt.

Hierdoor blijven de labels ongeveer dezelfde grootte in schermpixels, onafhankelijk van het zoomniveau.

Dit verhoogt de leesbaarheid sterk.

---

## 28.1 Screenshots

> **Screenshot toevoegen:** steden bij lage zoom.

> **Screenshot toevoegen:** steden rond 5x à 10x zoom.

> **Screenshot toevoegen:** steden bij ongeveer 20x zoom met kleinere towns.

---

# 29. R-tree

Alle OSM-steden worden vooraf opgenomen in een spatial index.

Hiervoor wordt NetTopologySuite gebruikt.

De concrete datastructuur is:

```text
STRtree<City>
```

Elke stad wordt toegevoegd met een geografische envelope op basis van longitude en latitude.

---

# 30. Viewport-query

Wanneer steden getoond moeten worden, wordt niet eerst door alle 2.453 steden gegaan.

De geografische grenzen van de zichtbare viewport worden bepaald.

Vervolgens wordt de spatial index bevraagd:

```csharp
_citySpatialIndex.Query(
    minLongitude,
    minLatitude,
    maxLongitude,
    maxLatitude);
```

Hierdoor worden alleen steden die geografisch relevant zijn voor de huidige viewport kandidaat voor rendering.

---

# 31. R-tree experiment

Een aparte vergelijking werd uitgevoerd tussen:

- spatial query via de R-tree;
- brute-force filtering.

Een testviewport rond België gebruikte:

```text
Longitude: 2,5 .. 6,5
Latitude: 49,4 .. 51,6
```

Resultaat:

```text
580 steden
```

De R-tree en brute-force methode leverden exact dezelfde steden op.

---

## 31.1 Gemeten tijden

| Onderdeel | Gemeten tijd |
|---|---:|
| R-tree opbouwen | 18,127 ms |
| R-tree query | 1,307 ms |
| Brute-force query | 0,766 ms |

---

# 32. Analyse van R-tree versus brute force

In deze specifieke test was brute force iets sneller dan de R-tree query.

Dit lijkt eerst verrassend.

De reden is dat de totale dataset maar ongeveer:

```text
2.453 steden
```

bevat.

Een eenvoudige lineaire loop over 2.453 items is voor een moderne computer zeer goedkoop.

Een R-tree heeft daarentegen extra overhead door:

- de boomstructuur;
- envelopes;
- traversatie van nodes.

Voor deze kleine dataset kan de overhead dus groter zijn dan de winst.

---

## 32.1 Waarom toch een R-tree?

Bij veel grotere datasets verandert dit.

Wanneer bijvoorbeeld honderdduizenden of miljoenen geografische objecten aanwezig zijn, is het niet efficiënt om bij iedere viewportwijziging alle objecten te controleren.

Een spatial index kan dan grote delen van de dataset onmiddellijk uitsluiten.

Daarom blijft de R-tree architecturaal de betere schaalbare oplossing.

---

# 33. Controle dat de viewport werkelijk gebruikt wordt

Tijdens het pannen en zoomen werden de bounds en resultaten tijdelijk gelogd.

Voorbeelden:

```text
Lon 5,33 .. 44,10 → 1089 steden

Lon 7,98 .. 46,75 → 375 steden

Lon 9,71 .. 48,49 → 0 steden

Lon 3,94 .. 42,71 → 1485 steden
```

Het aantal resultaten verandert dus effectief wanneer de viewport verandert.

Hiermee werd gecontroleerd dat de R-tree werkelijk op basis van de huidige kaartviewport bevraagd wordt.

---

# 34. UX en gebruikersinterface

De applicatie toont bovenaan duidelijke controls voor:

- projectiekeuze;
- datasetkeuze;
- polygon reduction-statistieken.

Daarnaast wordt statusinformatie getoond, zoals:

- zoomniveau;
- aantal landen;
- aantal zichtbare steden;
- longitude;
- latitude;
- loadtijd;
- rendertijd.

Hierdoor worden geen onduidelijke losse getallen zonder context weergegeven.

---

# 35. Performance-informatie in de GUI

Tijdens het laden wordt de verwerkingstijd gemeten met een `Stopwatch`.

Ook kaart-rendering en zoomoperaties worden gemeten.

Voorbeeld:

```text
Load: 125.42 ms
Render: 45.31 ms
Zoom: 2.49x
```

De exacte tijden hangen af van:

- gekozen dataset;
- computer;
- huidige zoom;
- JIT;
- caching;
- aantal zichtbare objecten.

---

# 36. Extra functionaliteit

Naast de minimale functionaliteit werden verschillende extra onderdelen toegevoegd.

---

## 36.1 Interactieve polygon reduction

De gebruiker kan live wisselen tussen:

```text
Original
RDP 0.020
VW 0.050
```

Hierdoor kan tijdens de demonstratie direct worden vergeleken hoe de reduction het visuele resultaat beïnvloedt.

---

## 36.2 Dataset-statistieken

Bij iedere dataset worden gegevens getoond zoals:

```text
Punten
Reductiepercentage
Bestandsgrootte
```

Voorbeeld:

```text
Punten: 53.271 | Reductie: 46,50% | Bestand: 2,20 MB
```

---

## 36.3 Azimuthal Equidistant

Naast Web Mercator en Lambert 2008 werd een fundamenteel andere azimutale projectie toegevoegd.

Hierdoor kan de invloed van verschillende projectietypes duidelijk visueel worden vergeleken.

---

## 36.4 City balancing

De city-weergave gebruikt meerdere technieken tegelijk:

- viewport filtering;
- R-tree;
- zoomniveau;
- population;
- hoofdstadstatus;
- place type;
- city priority;
- maximumaantal labels;
- collision detection.

Dit gaat verder dan simpelweg alle steden op het scherm tekenen.

---

## 36.5 Constante visuele lijndikte

Landsgrenzen worden gecorrigeerd voor het zoomniveau zodat zij niet extreem dik worden bij sterke zoom.

Dit is een extra UX-optimalisatie.

---

# 37. Algemene experimentatie

Tijdens de ontwikkeling werden verschillende onderdelen afzonderlijk getest.

---

## 37.1 Polygon reduction experimenten

Voor RDP en VW werden meerdere parameters gebruikt.

Bij iedere test werden gemeten:

- originele punten;
- resterende punten;
- reductionpercentage;
- rekentijd;
- bestandsgrootte.

Hiermee kon een geschikte balans tussen detail en performance gekozen worden.

---

## 37.2 Projectietesten

Voor iedere projectie werd getest of:

```text
lon/lat
→ projectie
→ inverse projectie
```

weer de oorspronkelijke geografische positie oplevert.

Voor Lambert werden hiervoor onder andere gebruikt:

- Brussel;
- Gent;
- Antwerpen.

---

## 37.3 SLERP-test

Voor SLERP werd bewust een lange route gebruikt:

```text
Honolulu → Nairobi
```

Hierdoor worden veel mogelijke problemen zichtbaar, waaronder de kaartnaad rond de antimeridiaan.

---

## 37.4 City filtering experimenten

De city thresholds werden tijdens ontwikkeling meerdere keren aangepast.

Het doel was een balans te vinden tussen:

- belangrijke steden zichtbaar houden;
- ook kleinere steden tonen bij hoge zoom;
- niet te veel labels tegelijk tonen;
- label-overlap vermijden.

---

## 37.5 R-tree experiment

De R-tree werd niet alleen geïmplementeerd maar ook vergeleken met brute force.

Hieruit bleek dat een R-tree bij een kleine dataset niet noodzakelijk sneller is, maar wel beter schaalbaar is voor grote datasets.

---

# 38. Belangrijkste resultaten

Uit de experimenten kunnen enkele belangrijke resultaten worden afgeleid.

### Polygon reduction

Een reduction van ongeveer 50% kan worden toegepast zonder grote visuele verschillen op normale schaal.

### Projecties

Dezelfde geografische gegevens kunnen zeer verschillend worden weergegeven afhankelijk van de gekozen projectie.

### SLERP

De grotecirkelroute moet in geografische ruimte berekend worden en niet rechtstreeks als lijn op het scherm.

### OSM

Alle steden tegelijk tonen geeft geen bruikbare kaart. Filtering en prioritering zijn noodzakelijk.

### R-tree

Een spatial index is niet automatisch sneller voor een kleine dataset, maar biedt voordelen voor schaalbaarheid.

---

# 39. Conclusie

Voor deze opdracht werd een volledige interactieve WPF-wereldkaart ontwikkeld.

De applicatie combineert verschillende technieken uit spatial programming:

- GeoJSON;
- geometrische vereenvoudiging;
- kaartprojecties;
- inverse projecties;
- pan en zoom;
- SLERP;
- OpenStreetMap;
- spatial indexing;
- collision detection.

De polygon-reductionexperimenten tonen dat het aantal geometriepunten aanzienlijk kan worden verminderd zonder onmiddellijk een grote visuele kwaliteitsvermindering te veroorzaken.

RDP 0.020 reduceert bijvoorbeeld:

```text
99.566 → 51.459 punten
```

terwijl de kaart op wereldniveau visueel zeer herkenbaar blijft.

VW 0.050 bereikt een vergelijkbare reduction:

```text
99.566 → 53.271 punten
```

maar behoudt sommige natuurlijke vormen anders dan RDP.

De implementatie van drie verschillende projecties toont daarnaast duidelijk dat kaartprojecties verschillende soorten vervorming veroorzaken.

Door SLERP vóór de projectie uit te voeren, blijft de vogelvluchtroute geografisch dezelfde route in alle drie projecties.

Voor steden bleek een combinatie van R-tree, viewport-filtering, zoomafhankelijke filtering, prioritering en collision detection noodzakelijk om een leesbare kaart te verkrijgen.

Ten slotte zorgt de opsplitsing in:

```text
Core
Infrastructure
App
PolygonReducer
```

samen met Dependency Injection en MVVM voor een duidelijkere en onderhoudbare architectuur.

---

# 40. Bronvermelding

## 40.1 GeoJSON en geografische data

- Natural Earth — Admin 0 Countries  
  https://www.naturalearthdata.com/

- geojson.xyz  
  https://geojson.xyz/

- GeoJSON RFC 7946  
  https://datatracker.ietf.org/doc/html/rfc7946

---

## 40.2 Kaartprojecties

- EPSG Geodetic Parameter Dataset  
  https://epsg.io/

- Belgian Lambert 2008 — EPSG:3812  
  https://epsg.io/3812

- Web Mercator — EPSG:3857  
  https://epsg.io/3857

- Snyder, J.P. — Map Projections: A Working Manual  
  USGS Professional Paper 1395  
  https://pubs.usgs.gov/pp/1395/report.pdf

---

## 40.3 OpenStreetMap

- OpenStreetMap  
  https://www.openstreetmap.org/

- Overpass Turbo  
  https://overpass-turbo.eu/

- Overpass API documentation  
  https://wiki.openstreetmap.org/wiki/Overpass_API

- OSM `place=*`  
  https://wiki.openstreetmap.org/wiki/Key:place

- OSM `population=*`  
  https://wiki.openstreetmap.org/wiki/Key:population

- OSM `capital=*`  
  https://wiki.openstreetmap.org/wiki/Key:capital

---

## 40.4 Libraries

- NetTopologySuite  
  https://github.com/NetTopologySuite/NetTopologySuite

- OsmSharp  
  https://github.com/OsmSharp/core

- GeoJSON.NET  
  https://github.com/GeoJSON-Net/GeoJSON.Net

- Newtonsoft.Json  
  https://www.newtonsoft.com/json

---

## 40.5 Microsoft

- Microsoft Learn — WPF  
  https://learn.microsoft.com/en-us/dotnet/desktop/wpf/

- Microsoft Learn — Dependency Injection  
  https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection

---

## 40.6 Ontwikkelondersteuning

- OpenAI ChatGPT  
  https://chatgpt.com/

ChatGPT werd gebruikt als ondersteuning tijdens ontwikkeling, debugging, analyse en documentatie. De implementatie en resultaten werden in de eigen applicatie getest en gecontroleerd.

---

# 41. Screenshots voor definitieve versie

Voor de definitieve rapportversie worden de volgende screenshots toegevoegd:

1. wereldkaart bij startup met `RDP 0.020`;
2. Original dataset;
3. RDP 0.020 op dezelfde locatie;
4. VW 0.050 op dezelfde locatie;
5. Web Mercator;
6. Lambert 2008 rond België;
7. Azimuthal Equidistant;
8. dezelfde SLERP-route in Web Mercator;
9. dezelfde SLERP-route in Lambert 2008;
10. dezelfde SLERP-route in Azimuthal Equidistant;
11. city labels bij lage zoom;
12. city labels bij middelhoge zoom;
13. city labels bij ongeveer 20x zoom;
14. PolygonReducer Console-output.

---

# 42. Eindoverzicht

De uiteindelijke implementatie ondersteunt:

- [x] GeoJSON inlezen;
- [x] opgevulde landsgrenzen weergeven;
- [x] pan zonder GIS-library;
- [x] zoom zonder GIS-library;
- [x] laad- en rendertijd tonen;
- [x] polygon reduction met NetTopologySuite;
- [x] Ramer-Douglas-Peucker;
- [x] Visvalingam-Whyatt;
- [x] aparte PolygonReducer Console-app;
- [x] Web Mercator;
- [x] Belgian Lambert 2008;
- [x] Azimuthal Equidistant;
- [x] inverse projectie;
- [x] longitude en latitude onder de muis;
- [x] twee punten aanklikken;
- [x] grotecirkelroute via SLERP;
- [x] route in drie projecties;
- [x] OSM uitlezen met OsmSharp;
- [x] `city` en `town` verwerken;
- [x] population-tags verwerken;
- [x] capital-tags verwerken;
- [x] zoomafhankelijke city filtering;
- [x] kleinere towns bij sterke zoom;
- [x] collision detection;
- [x] R-tree via NetTopologySuite;
- [x] viewport-gebaseerde R-tree query;
- [x] Dependency Injection;
- [x] interfaces;
- [x] MVVM;
- [x] meerdere projecten/lagen;
- [x] interactieve Original/RDP/VW-vergelijking;
- [x] dataset-statistieken in de UI.

---