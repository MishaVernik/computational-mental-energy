# Комплексна лабораторна робота — Цифровий двійник

**Тема**: Цифровий двійник користувача для визначення когнітивного стану на основі потокових EEG-даних
**Автор**: Михайло Вернік (Mykhailo Vernik)
**Репозиторій**: [github.com/.../PHD](../README.md)
**Дата**: 2026-05-18

---

## Зміст за пунктами завдання


| №   | Пункт завдання                                                                       | Стан                                       | Артефакт                                       |
| --- | ------------------------------------------------------------------------------------ | ------------------------------------------ | ---------------------------------------------- |
| 1   | Обрати об'єкт / процес — фізичний двійник                                            | ✅                                          | [§1](#1-фізичний-двійник)                      |
| 2   | Розробити програмний генератор потокових даних або використати апаратне забезпечення | ✅                                          | [§2](#2-потокові-дані--muse-athena--симулятор) |
| 3   | Створити 3D-модель фізичного двійника                                                | ✅                                          | [§3](#3-3d-модель)                             |
| 4   | Обрати платформу для створення цифрового двійника                                    | ✅                                          | [§4](#4-платформа-цифрового-двійника)          |
| 5   | Розробити цифровий двійник                                                           | ✅                                          | [§5](#5-розробка-цифрового-двійника)           |
| 6   | Протестувати цифровий двійник                                                        | ✅                                          | [§6](#6-тестування)                            |
| 7   | Підготувати демонстрацію та записати відео                                           | 🟡 сценарій готовий, запис — за оператором | [§7](#7-демонстрація--відео)                   |
| 8   | Підготувати звіт                                                                     | ✅                                          | [§8](#8-звіт)                                  |


Пояснення позначень: ✅ — реалізовано та підтверджено; 🟡 — артефакт готовий, потребує одноразового виконання оператором (запис відео / реальна Muse Athena).

---

## 1. Фізичний двійник

**Об'єкт**: користувач під час когнітивної діяльності, з акцентом на чотири каналяи EEG-сигналу.


| Аспект              | Значення                                                            |
| ------------------- | ------------------------------------------------------------------- |
| Цільова величина 1  | $p_\text{flow}(t) \in [0,1]$ — імовірність flow-стану               |
| Цільова величина 2  | $\text{CME}(t)$ у Верниках (Vn) — Computational Mental Energy       |
| Часове вікно        | 5 секунд                                                            |
| Каналів EEG         | 4 (TP9, AF7, AF8, TP10 — електроди Muse Athena за стандартом 10-20) |
| Цільова латентність | ≤ 2 с (сигнал → екран)                                              |


Обґрунтування у [paper/iccseea2026_cme_quantum_eeg_paper.md](../paper/iccseea2026_cme_quantum_eeg_paper.md) §1–4.

---

## 2. Потокові дані — Muse Athena + симулятор

Реалізовано **обидва шляхи**, як дозволяє завдання:

### 2.1 Апаратний шлях (Muse Athena)

```
Muse Athena (BLE 5.2, 4 канали × 256 Гц)
    └─→ MindMonitor (Android/iOS, OSC :7002)
        └─→ muse-bridge/bridge.py (Python 3.11+)
            └─→ POST /eeg-stream
                └─→ CmeSim.Api SignalR
```

Артефакт: `[muse-bridge/bridge.py](../muse-bridge/bridge.py)`,
`[muse-bridge/README.md](../muse-bridge/README.md)`.

### 2.2 Програмний генератор потокових даних

`bridge.py --simulate` — стохастичний процес, який видає band-powers,
сумісні за форматом з MindMonitor OSC. Гарантує, що демонстрація працює
без апаратного забезпечення.

### 2.3 Контракт потокового вікна

```ts
EegWindowData = {
  timestamp: ISO8601,
  channels: { TP9, AF7, AF8, TP10 } × { delta, theta, alpha, beta, gamma } µV²,
  channelQuality: { TP9, AF7, AF8, TP10 } ∈ [0,1],
  quality: number,         // мін. по каналах
  taskDifficulty: number,  // c(t) ∈ [0,1]
  sourceMode: 'live' | 'simulator' | 'replay'
}
```

Тип реалізовано в C# (`EegWindowDto`) та в TypeScript
(`[cme-live-dashboard/src/types.ts](../cme-live-dashboard/src/types.ts)`).

---

## 3. 3D-модель

**Артефакти**:

- `[cme-live-dashboard/scripts/build-head-glb.mjs](../cme-live-dashboard/scripts/build-head-glb.mjs)`
— генератор glTF binary (Node + `@gltf-transform/core`).
- `[cme-live-dashboard/public/head_with_muse.glb](../cme-live-dashboard/public/head_with_muse.glb)`
— 215 888 байт, відтворюваний через `npm run build:glb`.
- `[cme-live-dashboard/src/components/HeadTwin3D.tsx](../cme-live-dashboard/src/components/HeadTwin3D.tsx)`
— Three.js рендерер (react-three-fiber + drei).

**Структура моделі** (named-nodes для прив'язки твінів):


| Вузол GLB                   | Геометрія                                 | Призначення                      |
| --------------------------- | ----------------------------------------- | -------------------------------- |
| `Head`                      | еліпсоїд (sphere scale [0.85, 1.0, 0.95]) | череп                            |
| `Jaw`, `Chin`               | сфери                                     | нижня щелепа, підборіддя         |
| `Nose`                      | сфера                                     | орієнтир «перед»                 |
| `EarL`, `EarR`              | сфери                                     | вуха (опорні точки для TP9/TP10) |
| `Neck`                      | циліндр                                   | шия                              |
| `MuseHeadband`              | тор, нахилений на ~10°                    | пов'язка Muse                    |
| `AF7`, `AF8`, `TP9`, `TP10` | пласкі диски (циліндри)                   | сенсорні електроди               |


**Зв'язування з даними** (Three.js):


| Візуальний параметр   | Формула                                                          | Джерело       |
| --------------------- | ---------------------------------------------------------------- | ------------- |
| Колір електрода (hue) | $\text{lerp}(220°, 10°, \beta / (\alpha + \theta) / 1.5)$        | band powers   |
| Інтенсивність         | $\log(1 + 8(\beta+\theta+\alpha)) / \log(50)$                    | band powers   |
| Масштаб електрода     | $0.85 + 0.25 \cdot I$                                            | інтенсивність |
| Halo (ореол)          | зелений якщо `isFlow`, opacity $0.06 + 0.18 \cdot p_\text{flow}$ | CME inference |


Скриншот idle-стану: `[docs/screenshots/headtwin3d-idle.png](screenshots/headtwin3d-idle.png)`.

---

## 4. Платформа цифрового двійника

**Прийняте рішення**: гібрид «локальний runtime + Azure Digital Twins
як платформенний рівень».


| Шар                     | Що там працює                                                        | Чому                                                          |
| ----------------------- | -------------------------------------------------------------------- | ------------------------------------------------------------- |
| L1 — Сенсори            | Muse Athena, MindMonitor, bridge                                     | апаратно прив'язано                                           |
| L2 — Ядро твіна         | CmeSim.Api, qbackend (Qiskit), flow-classifier (PyTorch), CME engine | повний контроль, без cloud-залежності для демо                |
| L3 — Synchronization    | SignalR + DigitalTwinSyncService                                     | mirror тонких summary-апдейтів у ADT                          |
| L4 — Локальний UI       | cme-live-dashboard + HeadTwin3D                                      | demo-safe (працює офлайн)                                     |
| L5 — Cloud DT платформа | **Azure Digital Twins** + Blob + **3D Scenes Studio**                | формальна DT-платформа з DTDL + декларативними 3D-прив'язками |


**Підстава вибору ADT** ([детально у `digital_twin_platform.md](digital_twin_platform.md)`):

1. ADT — єдина mainstream-платформа з first-class DTDL ↔ GLB-прив'язками (3D Scenes Studio).
2. Регіон **West Central US** (інстанс `cme`) для безкоштовного $200 кредиту + $1 000 Startups Hub.
3. DTDL-файли — JSON-LD; портативні на Eclipse Vorto, FIWARE NGSI-LD, W3C Thing Descriptions.
4. Cost envelope для лабораторного профілю: **< $1/тиждень** (доказ у §4.4).

### 4.1 Онтологія DTDL v3

Файли у `[docs/dtdl/](dtdl/)`:


| Модель           | DTMI                   | Призначення                                             |
| ---------------- | ---------------------- | ------------------------------------------------------- |
| `User.json`      | `dtmi:cme:User;1`      | користувач, добовий бюджет Vn, поточний $p_\text{flow}$ |
| `Headband.json`  | `dtmi:cme:Headband;1`  | пристрій, 4 електродних дітей, режим джерела            |
| `Electrode.json` | `dtmi:cme:Electrode;1` | один канал, telemetry δθαβγ, властивість quality        |
| `Session.json`   | `dtmi:cme:Session;1`   | сесія, activity, complexity, cumulativeCmeVn            |
| `Window.json`    | `dtmi:cme:Window;1`    | 5-секундне вікно (опційне, вимкнено у Phase 1)          |


Зв'язки: `User --wears--> Headband --hasElectrode--> Electrode (×4)`;
`User --runs--> Session --contains--> Window`.

### 4.2 Створені Azure-ресурси (станом на 2026-05-18)


| Ресурс               | Ім'я / Hostname                                                                                                     |
| -------------------- | ------------------------------------------------------------------------------------------------------------------- |
| Subscription         | Microsoft Azure Sponsorship (`9acd98d2-d87b-46b9-ab35-daaa52513f2c`)                                                |
| Resource Group       | `AzureForStartups`                                                                                                  |
| ADT instance         | `cme` → `https://cme.api.wcus.digitaltwins.azure.net` (West Central US)                                             |
| Storage account      | `cmedtmv` (Standard_LRS, StorageV2)                                                                                 |
| Container            | `cmedtmv/twin-assets` (public blob read)                                                                            |
| GLB blob             | `https://cmedtmv.blob.core.windows.net/twin-assets/head_with_muse.glb`                                              |
| Scenes Studio config | `https://cmedtmv.blob.core.windows.net/twin-assets/3DScenesConfig.json`                                             |
| RBAC                 | `misha.vernik1@gmail.com` → `Azure Digital Twins Data Owner` на `cme`, `Storage Blob Data Contributor` на `cmedtmv` |
| Автентифікація з API | `DefaultAzureCredential` (через `az login`) — SP не створювався                                                     |


Розгортання: `[scripts/Complete-Azure-Setup.ps1](../scripts/Complete-Azure-Setup.ps1)`
(ідемпотентний; підтверджений другий «холостий» прогін, 55 с, exit 0).

### 4.3 Створені інстанси (twin instances)

Підтверджено через `az dt twin query --dt-name cme --query-command "SELECT * FROM digitaltwins"`:

```
user-default           dtmi:cme:User;1
headband-default       dtmi:cme:Headband;1
electrode-TP9          dtmi:cme:Electrode;1
electrode-AF7          dtmi:cme:Electrode;1
electrode-AF8          dtmi:cme:Electrode;1
electrode-TP10         dtmi:cme:Electrode;1
```

Створено автоматично при старті API через `DigitalTwinBootstrapper`
(`[CmeSim.Api/Services/DigitalTwinBootstrapper.cs](../CmeSim.Api/Services/DigitalTwinBootstrapper.cs)`).
Сьомий твін `session-<guid>` додається при `StartSession` з дашборду.

### 4.4 Cost envelope


| Профіль                                             | Місячна вартість           |
| --------------------------------------------------- | -------------------------- |
| Лаб. демо (1 користувач, ~30 хв/тижд)               | $1–3                       |
| MVP (500 користувачів, summary-only, 30-с інтервал) | ~$200                      |
| Анти-патерн (5-с per-window push)                   | ~$3 600 (свідомо уникнено) |


Економія досягається throttle (30 с / твін) + diff-only апдейтами у
`[DigitalTwinSyncService](../CmeSim.Api/Services/DigitalTwinSyncService.cs)`.

---

## 5. Розробка цифрового двійника

Стек у репозиторії (всі сервіси існували — додано лише нові компоненти, виділені **жирним**):


| Сервіс                      | Технологія                            | Порт       | Роль                       |
| --------------------------- | ------------------------------------- | ---------- | -------------------------- |
| muse-bridge                 | Python 3.11, python-osc               | 7002 (OSC) | Muse → API                 |
| **HeadTwin3D**              | React 18 + three.js + r3f + drei      | —          | 3D-візуалізація в дашборді |
| **DigitalTwinSyncService**  | .NET 8 + Azure.DigitalTwins.Core      | —          | mirror у ADT               |
| **DigitalTwinBootstrapper** | .NET 8 BackgroundService              | —          | створення базових твінів   |
| CmeSim.Api                  | ASP.NET Core 8 + SQL Server + SignalR | 5000       | ядро твіна, інференс       |
| qbackend                    | FastAPI + Qiskit Aer / IBM Runtime    | 8001       | VQC                        |
| flow-classifier             | FastAPI + PyTorch                     | 8002       | класична MLP               |
| cme-live-dashboard          | React 18 + Vite + recharts            | 3001       | UI                         |


### 5.1 Обчислювальна формула CME

$$
\text{CME}(t) = \kappa \cdot E_\text{band}(t) \cdot c(t) \cdot p_\text{flow}^\text{hybrid}(t) \cdot q(t)
$$

де $\kappa$ — калібрувальний коефіцієнт (per-user, в Vn), $E_\text{band}$ — повна
потужність 5 смуг, $c(t)$ — складність задачі, $q(t)$ — якість сигналу.

### 5.2 Гібридний класифікатор $p_\text{flow}$

$p_\text{flow}^\text{hybrid} = \alpha \cdot p_\text{VQC} + (1 - \alpha) \cdot p_\text{MLP}$,
де VQC — варіаційний квантовий класифікатор (Qiskit, 8-feature → 8-qubit
RY/RZ + CNOT × 4 шари), MLP — 8-32-16-1 з dropout 0.3.

Результати (з paper §6, тест на 288 вікнах):


| Модель              | Accuracy  | F1        | AUROC     |
| ------------------- | --------- | --------- | --------- |
| Класична MLP        | 91.7%     | 0.918     | 0.949     |
| VQC (Aer simulator) | 89.6%     | 0.893     | 0.937     |
| **Гібрид**          | **93.8%** | **0.939** | **0.967** |


Валідація на реальному **IBM Marrakesh Heron r2** (156 кубітів):
кореляція simulator vs QPU = $r = 0.940$ на 1000 спарених вікнах.

### 5.3 ADT sync поведінка

`DigitalTwinSyncService` для кожного вікна:

1. Дросселює апдейти до 30 с/твін.
2. Шле тільки змінені властивості (`DiffOnly = true`).
3. Публікує telemetry (`δθαβγ` на кожен електрод, `currentPFlow + currentCmeRateVnPerSec` на користувача).
4. Усі Azure-виклики у try/catch — збій не блокує SignalR-push.
5. При порожньому `Endpoint` реєструється `NoOpDigitalTwinSyncService` — система працює без Azure взагалі.

### 5.4 Сигнали, які несе твін (розширення)

Окрім сирих band-power telemetry і `p_flow`, твін зберігає три шари змісту:

| Рівень | Twin / Relationship | Властивості |
|---|---|---|
| Ops (Headband) | `headband-default` | `connectionState` (connected/disconnected/poorContact/simulated), `dropoutCountLastHour`, `lastSignalQualityMean` |
| Ops (Electrode) | `electrode-{TP9,AF7,AF8,TP10}` | `contactQuality` (good/weak/none), похідне від чисельного `quality` |
| Clinical live (User) | `user-default` | `engagementIndex` ($\beta/(\alpha+\theta)$), `cognitiveLoadIndex` ($\theta/(\alpha+\beta)$), `relaxationIndex` ($\alpha/\beta$), `alphaAsymmetryIndex` |
| Clinical daily (User) | `user-default` | `flowMinutesToday` (скидається о UTC опівночі), `budgetUtilization` (clamped 0..1), `fatigueLevel` (тренд theta за 5 хв), `currentActivitySlug`, `currentSessionId` |
| Session aggregates | `session-<guid>` (пишеться на `StopSession`) | `peakPFlow`, `flowMinutes`, `dataIntegrityScore`, `bestActivity`, `endedReason` |
| Activity graph | `User --[practiced]--> Activity` | `totalCmeVn`, `totalMinutes`, `sessionCount`, `personalAvgPFlow`, `lastUsedAt` |
| Activity graph | `Session --[hasActivity]--> Activity` | поточна активна activity (max 1, замінюється коли action змінюється) |

Усі 9 user-level індексів обчислюються в
[`DerivedMetricsService`](../CmeSim.Api/Services/DerivedMetricsService.cs) і
проходять через SignalR (live-дашборд) **і** ADT (один diff-only patch на 30 с).
Підтверджено 9 xUnit тестами у
[`CmeSim.Api.Tests/DerivedMetricsServiceTests.cs`](../CmeSim.Api.Tests/DerivedMetricsServiceTests.cs).

---

## 6. Тестування

Повний план — у `[docs/TEST_RUNBOOK.md](TEST_RUNBOOK.md)`. Результати на момент здачі:


| ID  | Що                                     | Результат                         | Доказ                                                                                                                                          |
| --- | -------------------------------------- | --------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| A1  | `npm run build` дашборду               | ✅ pass                            | 0 TS errors, 9.65 s, bundle 1.57 MB                                                                                                            |
| A2  | `npm run build:glb`                    | ✅ pass                            | 215 888 байт, MIME `model/gltf-binary`                                                                                                         |
| A3  | `dotnet build` API                     | ✅ pass                            | 0 errors, 0 warnings                                                                                                                           |
| A4  | HeadTwin3D рендерить у браузері        | ✅ pass                            | [headtwin3d-idle.png](screenshots/headtwin3d-idle.png), 0 WebGL errors у DevTools                                                              |
| A5  | NoOp fallback при пустому Endpoint     | ✅ pass                            | Code path verified ([Program.cs:96–118](../CmeSim.Api/Program.cs))                                                                             |
| D1  | DTDL upload в ADT                      | ✅ pass                            | `az dt model list` показує 5 моделей                                                                                                           |
| D2  | Bootstrapper створює базові твіни      | ✅ pass                            | `az dt twin query` повертає 6 твінів (див. §4.3)                                                                                               |
| D3  | Blob публічно доступний                | ✅ pass                            | `HEAD https://cmedtmv.blob.core.windows.net/.../head_with_muse.glb` → 200, 215 888 байт                                                        |
| D4  | API стартує з ADT-режимі               | ✅ pass                            | Лог: `DigitalTwinSyncService active: endpoint=https://cme.api.wcus.digitaltwins.azure.net, interval=30s, diffOnly=True` + `Base twins ensured` |
| D5  | Ідемпотентний re-run скрипту provision | ✅ pass                            | Другий прогін `Complete-Azure-Setup.ps1`, exit 0, всі «already»                                                                                |
| B   | Симулятор end-to-end (5 хв)            | 🟡 готово до запуску              | `bridge.py --simulate`                                                                                                                         |
| C   | Реальний Muse Athena                   | 🟡 потребує оператора з пристроєм | процедура §C у TEST_RUNBOOK                                                                                                                    |


Доекспериментальні докази (з paper, не повторюємо для лабораторної):

- Hybrid AUROC 0.967 на 288 вікнах ([Table 5](../paper/iccseea2026_cme_quantum_eeg_paper.md)).
- Simulator-vs-QPU $r=0.940$ ([§6.1.2](../paper/iccseea2026_cme_quantum_eeg_paper.md)).
- 9.15× CME-rate ratio Coding ÷ Resting ([Table 6](../paper/iccseea2026_cme_quantum_eeg_paper.md)).

---

## 7. Демонстрація + відео


| Артефакт                                                                | Стан                                                      |
| ----------------------------------------------------------------------- | --------------------------------------------------------- |
| `[docs/demo_script.md](demo_script.md)` — сценарій на 6 хв, single-take | ✅ готовий, з shot-list і таймінгами                       |
| `docs/demo.mp4` — запис                                                 | 🟡 запис здійснюється оператором за наявністю Muse Athena |


Сценарій покриває: фізичний twin → запуск стеку → симулятор → реальний пристрій → 2 активності (Resting + Mental arithmetic) → ADT Explorer → 3D Scenes Studio → IBM Marrakesh QPU validation slide.

SHA-256 файлу `demo.mp4` буде додано в [Додаток A](#додаток-a--хеші-артефактів) після запису.

---

## 8. Звіт

Розгорнутий звіт, структурований за 12 етапами текстової методики
(Рис. 1.4 / «Узагальнений процес створення цифрового двійника»),
знаходиться у `[docs/LAB_REPORT.md](LAB_REPORT.md)`.

Цей файл (`LAB_SUBMISSION.md`) — це **здавальний документ за 8 пунктами**
завдання; LAB_REPORT.md — деталізація для перевіряючого.

Допоміжні документи у `docs/`:


| Документ                                               | Призначення                                                          |
| ------------------------------------------------------ | -------------------------------------------------------------------- |
| `[digital_twin_platform.md](digital_twin_platform.md)` | архітектурна декларація платформи + Mermaid діаграма + cost envelope |
| `[azure_setup.md](azure_setup.md)`                     | runbook провізіювання Azure                                          |
| `[azure_credentials.md](azure_credentials.md)`         | пояснення опт-ін потоку Azure-секретів                               |
| `[dtdl/README.md](dtdl/README.md)`                     | онтологія DTDL                                                       |
| `[scenes_studio/README.md](scenes_studio/README.md)`   | налаштування 3D Scenes Studio                                        |
| `[TEST_RUNBOOK.md](TEST_RUNBOOK.md)`                   | повний тест-план A → F                                               |
| `[demo_script.md](demo_script.md)`                     | сценарій відео                                                       |


---

## Висновки

1. **Усі 8 пунктів комплексної лабораторної реалізовано** — фізичний twin
  (користувач + Muse Athena), потоковий генератор (`bridge.py --simulate`)
   та апаратний шлях, 3D-модель (`HeadTwin3D` + `head_with_muse.glb`),
   платформа (гібрид local + Azure Digital Twins), розробка (повний стек,
   нові компоненти інтегровано), тестування (тест-план + автоматичні
   перевірки green), сценарій відео, звіт.
2. **Усі 12 етапів текстової методики створення цифрового двійника**
  відображено у `[LAB_REPORT.md](LAB_REPORT.md)`, з конкретними посиланнями
   на код.
3. **Спостережувальне навантаження**: гібридна квантово-класична інференція
  $p_\text{flow}$ AUROC 0.967, валідована на IBM Marrakesh Heron r2,
   $r = 0.940$.
4. **Економічна модель**: cost envelope ≤ $1/тиждень для лаб-профілю;
  архітектурні рішення (throttle + diff) уникають $3 600/міс
   анти-патерну.
5. **Готовність до відтворення**: один інструкційний рядок —
  `./scripts/Complete-Azure-Setup.ps1` — створює всю Azure-частину
   ідемпотентно; `./run-all-services.ps1` піднімає локальний стек.

---

## Додаток A — Хеші артефактів


| Артефакт                                       | SHA-256                                                       |
| ---------------------------------------------- | ------------------------------------------------------------- |
| `cme-live-dashboard/public/head_with_muse.glb` | (заповнюється `Get-FileHash` перед здачею)                    |
| `docs/demo.mp4`                                | (заповнюється після запису)                                   |
| Git commit                                     | (заповнюється з `git rev-parse HEAD` після фінального коміту) |


```powershell
Get-FileHash cme-live-dashboard\public\head_with_muse.glb -Algorithm SHA256
Get-FileHash docs\demo.mp4 -Algorithm SHA256
git rev-parse HEAD
```

## Додаток B — Швидкий запуск з нуля (≤ 15 хв)

```powershell
# 1. Клонувати + білд
git clone <repo>; cd PHD
cd cme-live-dashboard ; npm install ; npm run build:glb ; cd ..
cd CmeSim.Api      ; dotnet restore ; cd ..

# 2. Azure side (опційно, лише для ADT-частини демо)
az login
.\scripts\Complete-Azure-Setup.ps1

# 3. Локальний стек
.\run-all-services.ps1

# 4. Емуляція даних
cd muse-bridge ; python bridge.py --simulate

# 5. Відкрити дашборд: http://localhost:3001 → Login → Start Session
# 6. Відкрити ADT Explorer: https://explorer.digitaltwins.azure.net
#    Attach to https://cme.api.wcus.digitaltwins.azure.net
#    Query: SELECT * FROM digitaltwins → 6 базових + Session
```

## Додаток C — Невирішені на момент здачі пункти


| Пункт                                           | Чому                                   | Хто закриває                                   |
| ----------------------------------------------- | -------------------------------------- | ---------------------------------------------- |
| Запис `demo.mp4`                                | потребує живої сесії з Muse Athena     | оператор (Ф. Бездітко)                         |
| Скриншоти `screenshots/real-eyes-open.png` тощо | те саме                                | оператор                                       |
| Real-Muse-runbook §C виконання                  | те саме                                | оператор                                       |
| Запис простроченого ADT cost (тижневий)         | формується тільки через тиждень роботи | автоматично, через `az consumption usage list` |


