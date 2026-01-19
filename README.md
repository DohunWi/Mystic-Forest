# ⚔️ Mystic Forest

> **Unity 2D Action Platformer** > *1인 개발 프로젝트 (2025.12 ~ 2026.01)*

![Badge](https://img.shields.io/badge/Unity-6.3_LTS-black?style=flat&logo=unity)
![Badge](https://img.shields.io/badge/C%23-Scripting-blue?style=flat&logo=csharp)
![Badge](https://img.shields.io/badge/Platform-WebGL-orange?style=flat&logo=html5)
![Badge](https://img.shields.io/badge/License-MIT-green?style=flat)

<br>

## 🎮 Play the Game
별도의 설치 없이 웹 브라우저에서 바로 플레이할 수 있습니다.
> **👉 [Play on itch.io](https://dony-wi.itch.io/game)**
> **📹 [Game Play Video](https://www.youtube.com/watch?v=t2p8x0J4XqU)**

<br>

## 📝 Project Overview
**Mystic Forest**는 숲을 오염시키는 거대 몬스터를 처치하는 2D 횡스크롤 액션 게임입니다.  
단순한 조작으로 화려한 액션을 즐길 수 있도록 **타격감(Juice)** 구현에 집중했으며, 상태 머신(FSM)을 활용한 보스 AI와 최적화된 WebGL 빌드를 목표로 개발했습니다.

### 🎥 Gameplay Preview
| Combat & Parry | Boss Phase & Pattern | Victory Sequence |
| :---: | :---: | :---: |
| ![Combo Action](여기에_GIF_이미지_주소_넣기1) | ![Boss Pattern](여기에_GIF_이미지_주소_넣기2) | ![Ending](여기에_GIF_이미지_주소_넣기3) |
| *3단 콤보 및 패링 시스템* | *부유형 보스 패턴 및 광폭화* | *엔딩 및 슬로우 모션 연출* |

<br>

## ✨ Key Features

### 1. Dynamic Combat System
- **3-Hit Combo:** 공격 버튼 연타 시 1타/2타/3타 애니메이션과 판정이 변화하며, 마지막 타격에 강력한 임팩트 부여.
- **Parry System:** 적의 공격 타이밍에 맞춰 방어 시 `Time.timeScale`을 조절하여 슬로우 모션 효과와 반격 기회 제공.

### 2. Intelligent Boss AI (FSM)
- **Deadzone Movement Logic:** 부유형 보스의 무게감을 위해, 플레이어와의 고도 차이가 임계값(Threshold)을 넘을 때만 Y축 이동을 수행하는 데드존 로직 적용.
- **3-Phase Patterns:** 체력에 따라 [Smash(지면 강타)] → [Magic(유도탄)] → [Thunder(광역기)]로 변화하는 패턴 구현.
- **Enrage Mode:** 체력 50% 이하 시 붉은 오라(Particle)와 함께 공격 속도가 빨라지는 광폭화 시스템.

### 3. Visual & Audio Polish (Game Juice)
- **Hit Stop:** 타격 성공 시 0.15초간 프레임을 정지시켜 물리적 저항감 표현.
- **Screen Shake:** `Cinemachine Impulse Source`를 활용한 동적 카메라 흔들림.
- **Post-Processing:**
    - **Chromatic Aberration:** 피니시 공격 및 피격 시 화면 찢어짐(Glitch) 효과.
    - **Bloom & Vignette:** 몽환적인 숲의 분위기와 시선 집중 유도.
- **Audio Mixing:** BGM과 SFX 채널을 분리하고 Audio Mixer를 통해 타격음이 묻히지 않도록 밸런싱.

<br>

## 🛠️ Tech Stack & Architecture

### Environment
- **Engine:** Unity 6.3 LTS(6000.3.2f1) (Universal Render Pipeline)
- **Language:** C#
- **IDE:** Visual Studio Code

### Core Technologies
- **Finite State Machine (FSM):** 보스의 상태(Idle, Chase, Attack, Dead)를 클래스로 관리하여 확장성 확보.
- **Singleton Pattern:** `GameManager`, `SoundManager`, `UIManager` 등 전역 관리 매니저 구현.
- **Observer Pattern (Action/Event):** 보스 사망 및 UI 업데이트 등 이벤트 기반의 느슨한 결합(Loose Coupling) 구조.
- **Cinemachine:** 카메라 추적 및 Impulse(충격) 효과 제어.
- **Unity Input System:** 키보드/마우스 입력 처리.

### Code Snippet: Boss Deadzone Logic
플레이어의 단순 점프에 보스가 과민 반응하는 것을 방지하기 위한 **데드존(Deadzone)** 로직입니다.
```csharp
// BossController.cs 중 일부
void MoveToTarget()
{
    // ... X축 이동 로직 생략 ...

    // 보스의 현재 고도(currentHoverY)와 이상적인 목표 고도(idealY)의 차이 계산
    float idealY = player.position.y + offsetFromPlayer.y;
    float difference = Mathf.Abs(idealY - currentHoverY);

    // 차이가 임계값(jumpThreshold)보다 클 때만(층간 이동 등) 목표 고도 갱신
    if (difference > jumpThreshold)
    {
        currentHoverY = idealY;
    }

    // SmoothDamp를 사용하여 묵직한 부유 움직임 구현
    transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime, moveSpeed);
}
```
<br>

## 📂 Installation
이 프로젝트는 **Unity 6.3 LTS** 버전으로 제작되었습니다.

1. Clone the repository:
   ```bash
   git clone [https://github.com/DohunWi/Mystic-Forest.git](https://github.com/DohunWi/Mystic-Forest.git)
2. Open in Unity Hub(Add project from disk).
3. Open **Scenes/TitleScene** to start.

<br>

## 👨‍💻 Author
### Dohun Wi
- Developer & Designer

- Game Play: [Play on itch.io](https://dony-wi.itch.io/game)

- Portfolio: https://www.notion.so/Mystic-Forest-2D-2cdb8e281c728063a15de9c46b3adb43?pvs=12

- Email: widohub7@gmail.com
