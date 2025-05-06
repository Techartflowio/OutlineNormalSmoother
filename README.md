# Unity Outline Normal Smoother
이 플러그인은 툰 아웃라인이 딱딱한 가장자리에서 끊어지는 문제를 빠르고 자동으로 해결하며, 사용자 정의가 가능하고 고성능(Jobs + Burst + Mathematics)으로 바로 사용할 수 있습니다.

![image-20241025163238785](./.assets/image-20241025163238785.png)

## 설치

1. Unity 버전이 2022.2 이상이어야 합니다.
2. Open your project
3. `Window > Package Manager > Add > Add package from git URL` , enter: `https://github.com/Techartflowio/OutlineNormalSmoother.git`
   - Github에서 Zip을 수동으로 다운로드하도록 선택할 수도 있습니다.: `Package Manager > Add package from disk`

## 사용

1. 모델 파일 이름에 `_Outline` 접미사를 추가합니다(예: `Cube_Outline.fbx`).
2. 모델을 Unity로 임포트하면 모델의 스트로크 노멀이 자동으로 계산되어 UV7번 체널에 저장됩니다.
3. 셰이더는 `모두의 셰이더 프로그래밍` 9장 학습 내용을 참조해야 합니다.
--------

## FAQ

여전히 추적 문제가 발생하는 경우 모델에 불법 데이터가 있는지 확인하세요.

- 누락되거나 잘못된 (0 / NaN) 노멀/탄젠트
- 잘못된 토폴로지
  - 동일한 삼각형의 여러 꼭지점이 일치하는 경우
  - 면적이 0인 삼각형
- 잘못 된 UV
  - 동일한 삼각형 UV의 여러 꼭지점이 일치하는 경우

--------
## 면책조항

- 본 스크립트는 Jason Ma가 공개한 Outline Normal Smoother 오픈소스를 기반으로 하며, 『모두의 셰이더 프로그래밍』 챕터 9의 실습을 위해 일부 코드가 수정되었습니다.
- 『모두의 셰이더 프로그래밍』 챕터 9 이외의 용도나 프로젝트에 해당 코드를 사용하는 경우, 의도하지 않은 렌더링 결과나 오류가 발생할 수 있으며, 이에 대해 저자는 어떠한 보증이나 책임을 지지 않습니다.
- 본 스크립트는 학습 목적의 예제 코드로 제공되며, 실제 프로젝트에 적용 시에는 사용자의 책임 하에 수정 및 검토가 필요합니다.