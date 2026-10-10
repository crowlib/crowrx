# CrowRx.Unity 코드 검토 — 성능, GC, 수명 관리

검토·수정일: 2026-10-10. 최초 분석 기준: `crowrx` HEAD `1cfc1d5`와 당시 미커밋 변경을 함께 포함한 작업 트리.

## 최종 일괄 수정과 전체 재검토

후속 API 정리: `TransformExtension.GetAllComponentsInHierarchy`와 전달용 `GameObjectExtension.GetAllComponentsInHierarchy`를 제거했다. 단일 계층 수집은 `GetComponentsInChildren(true, results)`를 직접 사용한다. Unity API는 기존 리스트 내용을 덮어쓰므로 여러 계층의 결과를 누적하려면 별도 임시 리스트로 수집한 뒤 `AddRange`해야 한다. 제거한 API의 계약·GC 비교 테스트 2개와 전용 보조 코드도 삭제했다. 프로젝트 C# 참조 0건, Unity 재컴파일 오류·경고 0건, 남은 TransformExtension PlayMode 테스트 2개 통과를 확인했다. 아래 06번 수정 기록은 제거 전의 이력이다.

2026-10-10의 현재 소스를 기준으로 Assets/CrowRx의 **런타임 20개·에디터 16개, 총 36개 C# 파일 전부**를 다시 읽고 사용처·콜백 수명·예외 처리·타입 이름·순회·풀·문자열 할당을 검토했다. 테스트 소스와 assembly 정의도 확인했다. 외부 DLL/패키지 전체, QV 소비자 프로젝트 및 실제 게임의 프레임 비용까지 검토한 것은 아니다. 아래 최초 분석은 이력이며 이 절의 결과가 현재 상태다.

### 이번 수정

| 대상 | 현재 처리 |
|---|---|
| TypeExtension | 자기 자신 제외 수정 유지. 선언 타입마다 제네릭 인수를 나눠 표시하고, 중첩 타입·일반 내부 타입·열린 제네릭·배열·포인터·by-ref를 처리한다. 짧은 이름은 마지막 타입의 자체 인수만 포함한다. 일반 타입은 StringBuilder를 만들지 않는 경로를 유지했다. |
| StringExtension / GetHierarchyPath | ToUpperFirst는 이미 대문자면 원본을 반환하고, 변경할 때 string.Create로 결과 하나를 만든다. 계층 경로는 풀의 이름 목록을 역순으로 결과에 한 번 복사한다. root 제외·자기 자신·무관한 root·빈 이름 계약을 유지했다. Unity의 name 조회와 결과 문자열의 할당은 남는다. |
| Mathm | RectIntersect의 bounds 4회 LINQ 순회를 한 번의 루프로 교체하고 bounds 제외를 먼저 검사한다. 둘레 순서의 정확히 4점이라는 인수를 명시·검사했다. IsArrival은 일반 입력의 제곱근·정규화를 생략하고 작은 거리 및 overflow/underflow 특수 경우를 기존 normalized 판정에 맞춰 처리한다. 다각형 포함 함수의 XY/XZ 투영을 주석으로 명시했다. |
| WhenAny / StateTuple | 모든 입력 완료와 초기 등록, 반환 결과의 단일 소비까지 끝나야 promise를 재사용한다. 한 카운터의 마지막 감소 경로만 반환해 늦은 콜백·동시 완료·재진입 시 새 호출 오염을 막는다. promise와 닫힌 StateTuple 타입별 유휴 풀은 각각 최대 64개로 제한하고 tuple의 참조를 비운다. 결과 version과 중복 소비 검사를 유지한다. |
| SerializedReferenceList | 직접 struct 순회 유지, 인터페이스는 List의 경로를 사용해 빈 목록의 공유 enumerator를 활용한다. List→wrapper 변환의 버리는 빈 List 생성을 제거했다. 양방향 null 변환은 null을 반환한다. 공유 목록 및 수정 감지·중첩 순회를 검사했다. |
| TryGetSingleComponent | 0개는 로그 없이 false, 1개는 true, 2개 이상은 기존 예외 로그와 false다. Component와 GameObject 경로를 함께 수정했다. |
| DebugUtility | DrawSphere가 반복 호출하는 DrawCircle의 꼭짓점별 Sin/Cos를 double 회전 점화식으로 교체했다. 마지막 선분은 첫 점으로 정확히 닫는다. 선 개수와 분할 수 계약은 유지하며 선 그리기 자체의 O(segments²) 비용은 남는다. |
| CancellationTokenSourceExtension | Cancel의 콜백이 예외를 던져도 finally에서 Dispose한다. 작업이 더 이상 소스를 사용하지 않을 때 소유자가 호출하는 계약을 주석으로 명시했다. |
| MonoBehaviourCrowRx | Awake 이전의 파괴 감시가 Play Mode 밖에서는 EditorApplication.update를 사용하도록 보완했다. 파괴된 managed wrapper의 실제 참조를 비우고 감시를 해제한다. 최초 취소 등록·감시 할당은 자동 정리의 비용으로 유지했다. |
| CanvasGroupFade / ContinueWithAnyway | 페이드 setter/getter를 인스턴스당 한 번 만들고, 왕복 페이드의 중복 오류 로깅을 제거했다. 완료 헬퍼의 캡처 람다와 중첩 async 래퍼를 제거하면서 성공/취소/실패 및 콜백 예외의 계약을 유지했다. |
| 계층 순회 / 에디터 문자열 캐시 | FindChildDeep, ExecuteEventToParent, SetLayerRecursively, ChildCirculateCallback의 자체 재귀를 제거했다. 기존 직접 일치 우선·전위 순회·부모의 가장 가까운 핸들러·하위 트리 제외를 유지한다. 에디터 MemberInfo 이름 캐시는 256개, 창의 문자열 타입 해석 캐시는 64개에서 비운다. |

호출자에게 영향을 주는 변경: RectIntersect의 잘못된 꼭짓점 수는 ArgumentException이다. FadeOutInAsync의 취소·실패는 이제 호출자에게 전파되며, void FadeInOut은 취소를 소비하고 실패를 한 번 로그한다. 중첩 제네릭 표시 문자열 및 null List 변환이 수정됐다. WhenAny 결과와 각 입력은 UniTask의 단일 소비 계약을 따른다. ChildCirculateCallback의 콜백에서 계층을 변경하지 않아야 한다. public API와 README에 해당 계약을 기록했다.

### 검증과 GC 측정의 한계

Unity 6000.6.5f1에서 컴파일 성공 및 **Play Mode 102개·Edit Mode 4개 통과, 두 모드 모두 실패·건너뜀 0**을 각각 Pipeline 완료 상태와 TestResults.xml에서 확인했다. Play Mode는 최초 77개에 새 25개를 추가했다. 중첩 타입 이름, 문자열 결과, 계층 경로·검색 우선순위·순회 제외, 부재/중복 로그, 네 꼭짓점·회전 사각형, 보간 주변 수학, 늦은 작업·fault·cancel·pool 한도·stale version·40회 4-worker 동시 완료, CTS 예외 시 정리, 페이드의 단일 로깅과 취소 전파, List 변환과 순회, 동기/비동기 워밍업 구간을 검사했다. Edit Mode는 기존 3개 에디터 회귀와 새 1개의 Play Mode 밖 파괴 정리를 검사했다. Editor update가 실행될 기회를 기다린 뒤 실제 참조가 null인지 확인했고, 관련 변경의 git diff --check도 통과했다.

**GC의 이전 0건 기록은 모든 managed 할당이 없다는 증거로 확대하지 않는다.** 이번 검증에서 GC.Alloc이 byte[1024]는 감지했지만, 알려진 참조 객체·문자열의 양성 대조군은 0건이었다. Profiler를 켜고 한 프레임 뒤 실행한 컴파일된 테스트에서도 array=1, reference=0, string=0이었다. 기존 문자열 조립과 새 조립도 모두 0건, 워밍업한 WhenAny도 0건으로 기록돼 이 환경의 계측만으로 reference/string 할당 감소량이나 완전한 무할당을 확인할 수 없다. 동적 eval의 GC.GetAllocatedBytesForCurrentThread 역시 byte[1024] 대조군이 0이라 사용하지 않았다. Profiler·Memory 영역 설정은 테스트 finally에서 원복했다.

따라서 불필요한 new/캡처/문자열 중간값 제거 및 풀 재사용은 소스와 소유권 테스트로 확인했지만 **전체 GC 바이트·CPU 개선율·실제 Player 성능은 미검증**이다. 아래 이전 절의 byte[] 교정 후 GC 0건 수치는 해당 marker의 관측값으로만 읽어야 한다. 신뢰할 수 있는 Player 계측에서 객체·문자열 양성 대조군도 먼저 검증해야 한다. DrawCircle 점화식은 컴파일·정적 기하 검토를 했고 그려진 픽셀이나 CPU 시간의 비교 검증은 하지 않았다.

### 전체 재검토에서 남긴 결론

이번 범위에서 새로 확인한 추가 기능 결함은 수정했으며, **이후 재검토에서 추가로 재현한 미수정 기능 오류는 없다.** 모든 입력·Unity 버전·게임 상황에 문제가 없다는 보장은 아니다. 다음은 유지하는 계약과 남는 정상 비용이다.

- 13의 FindObjectHelper는 사용자 결정대로 Obsolete 유지다. 기존 캐시의 중복 이름·무효화 문제를 고친 것은 아니므로 새로 사용하지 않는다. FindChildDeep도 중복 이름에서 첫 결과를 고르는 탐색이지 고유 식별 방법이 아니다.
- GetRealTypeFullName은 표시명이다. 다른 어셈블리의 같은 이름까지 구분하지 않는다. 최종 재컴파일 후 Editor 캐시의 2,092개 타입/이름에서 충돌 0개를 확인했지만, 외부 소비자가 식별자로 저장하려면 AssemblyQualifiedName 또는 실제 Type을 사용해야 한다. RuntimeTypeCache의 기존 이름 충돌 경고·첫 타입 유지 계약은 남긴다.
- 비어 있지 않은 IEnumerable 인터페이스 순회는 boxing한다. 기존 인터페이스를 없애거나 enumerator를 공유해 재진입·수명 안전성을 깨뜨리지 않는다. 핫패스는 구체 타입의 foreach 또는 인덱서를 사용한다.
- MonoBehaviour 캐시의 최초 취소 상태·등록·Awake 이전 감시, 페이드의 작업별 CTS, 풀의 첫 생성·확장, 타입 캐시의 첫 Reflection 수집, 결과 문자열 및 Unity name 반환 비용은 남는다.
- WhenAny는 입력 작업을 취소할 수 없다. 늦은 입력이 끝나지 않으면 그 콜백 상태가 살아 있는 것은 소유권 계약에 따른다. 유휴 풀 제한은 실행 중인 작업 수를 제한하지 않으며, 다중 스레드의 lock 경합은 별도 CPU 측정 대상이다. 결과와 입력을 중복 소비하지 않아야 한다.
- UnityEvent/콜백/커스텀 로거와 raycaster의 할당, 에디터의 SerializedProperty·IMGUI 엔진 비용, 높은 분할 수의 선 그리기 비용, Physics/native/layout 호출 비용은 소비자 코드와 실행 환경에 좌우된다. 코드만으로 실제 게임 병목이라고 단정하지 않는다.

## 현재 MonoBehaviourCrowRx 캐시 수정 결과

**캐시 구조를 수정했고 Unity 컴파일 및 전체 Play Mode 테스트 77개가 통과했다.** 아래 추가 관찰의 기존 R3 구독·RectTransform 검색 문제는 이 수정으로 처리했다.

- 처음 getter를 사용할 때 GameObject와 Transform을 함께 보관한다. rectTransform은 캐시한 Transform을 캐스팅하므로 일반 Transform에서도 반복 TryGetComponent를 하지 않는다. RectTransform 전용 캐시·구독과 별도 트리거 컴포넌트를 제거했다.
- Transform 종류를 바꾸면 InvalidateTransformCache()를 호출한다. 다음 transform/rectTransform 조회에서 현재 Transform을 가져오며 정리 등록을 추가하지 않는다. 비활성화·재활성화는 캐시를 유지한다.
- 하나의 destroyCancellationToken 등록으로 실제 참조와 등록을 정리한다. GameObject 파괴뿐 아니라 컴포넌트만 제거해도 동작하며, 파생 클래스의 OnDestroy에서 base 호출을 요구하지 않는다. 정리된 캐시는 다시 초기화되지 않는다.
- 실측에서 한 번도 Awake를 거치지 않은 컴포넌트는 파괴 시 Unity의 토큰이 취소되지 않았다. 따라서 이 경우에만 R3 Update 감시 객체를 등록한다. 활성화되면 감시를 종료하고, 활성화 전 파괴되면 다음 Update에서 캐시를 비운다. GameObject 및 컴포넌트 단독 파괴를 모두 검사하며 별도 MonoBehaviour를 추가하지 않는다. 활성화 전에는 프레임별 확인 비용이 있고 첫 접근에는 취소 상태·등록 및 필요 시 감시 객체의 할당이 남아 있다.

검증: Unity 6000.6.5f1에서 재컴파일 성공, Pipeline 완료 상태 및 TestResults.xml에서 **Play Mode 77개 통과, 실패·건너뜀 0**을 확인했다. 새 MonoBehaviourCacheTests의 12개는 일반/UI 객체 캐시, 명시적 Transform 갱신, 컴포넌트 단독 제거, 파생 OnDestroy, 활성화 전 파괴 및 활성화 후 감시 종료, 비활성화·재활성화, 초기화 전 파괴를 검사한다. 기존 65개도 통과했다. 관련 변경의 git diff --check가 통과했다.

GC: ProfilerRecorder의 GC.Alloc을 byte[1024] 생성으로 검증한 후 일반 Transform과 RectTransform 각각에서 워밍업한 세 getter를 100회 반복했고 **할당 이벤트 0건**이었다. 최초 초기화 비용 감소율·할당 바이트·CPU 시간·Player·다른 Unity 버전은 측정하지 않았다.

## 현재 10 수정 결과

**ExecuteEventExceptThis를 수정했고 Unity 컴파일 및 전체 Play Mode 테스트 65개가 통과했다.** 자신을 제외한 첫 유효 raycast 대상만 선택하고 해당 객체부터 부모 방향의 가장 가까운 이벤트 핸들러에 전달하는 계약을 유지했다. 그 대상이 처리하지 못해도 다음 raycast 결과를 시도하지 않는다. 이 종료 동작은 사용자가 확인한 메서드 역할이며 결함으로 분류하지 않는다.

- 입력 PointerEventData를 raycast와 전달에 그대로 사용한다. 별도의 PointerEventData 생성과 Input.mousePosition 접근을 제거해 좌표·pointerId·button·delta 및 데이터 하위 타입을 유지한다. 기존 BaseEventData 인수 형식은 유지하며 null·비포인터 데이터 또는 현재 EventSystem 부재 시 false를 반환한다. 비포인터 데이터의 기존 마우스 좌표 fallback은 제거했으며 XML 주석과 README에 이 계약을 명시했다.
- 결과 List를 ListPool<RaycastResult>에서 빌려 using 범위에서 반환하고 비운다. 중첩 호출은 각각 별도 대여 목록을 사용하며 raycaster 예외 시에도 목록을 반환한다. 무효 raycast 대상은 건너뛴다.
- 반환값은 선택된 객체/부모의 이벤트 핸들러 호출 여부다. raycast 개수만으로 true가 되는 경로를 제거했다. uGUI ExecuteEvents의 의미를 따르며 핸들러 자체의 예외는 설치된 uGUI가 로그 처리한다. 전체 RaycastAll과 정렬은 뒤쪽 대상을 찾기 위해 계속 수행한다.

검증: `Assets/Tests/Runtime/GameObjectEventTests.cs`의 11개와 기존 54개를 합쳐 **Play Mode 65개 통과, 실패·건너뜀 0**을 Pipeline 완료 상태와 Unity TestResults.xml에서 확인했다. 원본 포인터 데이터·메타데이터 유지, 가장 가까운 부모 전달, 첫 대상에서 종료, 자기 자신/무효/빈 결과, 비포인터 및 EventSystem 부재, 중첩 호출의 버퍼 분리, 예외 시 풀 반환, 실제 GraphicRaycaster와 겹친 Image 간 전달을 검사했다. 테스트 assembly의 UnityEngine.UI 참조를 추가했고 기존 EventSystem/raycaster 활성 상태와 임시 객체를 복구했다. GraphicRaycaster 검증은 합성 PointerEventData로 수행했으며 실제 마우스·터치·게임패드 장치 입력 전달을 검증한 것은 아니다.

GC: ProfilerRecorder의 GC.Alloc을 byte[1024] 생성으로 검증한 후 통제된 raycaster·클릭 핸들러·결과 2개 조건에서 워밍업하고 100회 전달했을 때 **할당 이벤트 0건**, 실제 처리 100회였다. 풀 최초 생성/용량 확장, 입력 데이터 생성, 실제 GraphicRaycaster 내부 및 사용자 핸들러의 비용까지 무할당임을 보장하지 않는다. CPU 시간·할당 바이트·Player는 측정하지 않았다. 관련 소스의 git diff --check도 통과했다.

## 현재 13 사용 중단 결정

사용자 결정에 따라 FindObjectHelper 클래스와 그 FindTargetObject 메서드, 이를 자동으로 사용하는 GameObjectExtension.FindTargetObject 확장 메서드를 Obsolete 경고 대상으로 지정했다. 기존 메서드 본문과 컴포넌트 파일·meta GUID는 유지하므로 기존 사용은 경고와 함께 계속 컴파일된다.

같은 이름의 객체가 여러 개면 어떤 객체를 찾을지 모호하며, 캐시된 객체가 살아 있고 이름·소속이 그대로여도 탐색 순서가 바뀌면 새 검색 결과와 달라질 수 있다. 따라서 단순한 유효성 검사 추가로 해결하지 않고 사용을 중단한다. 기존 객체는 직렬화된 직접 참조를, 런타임 생성 객체는 생성 시 보관한 참조를 사용하도록 안내했다. 상대 경로도 각 단계의 같은 부모 아래 이름이 유일해야 대상을 구분할 수 있다.

13의 내부 캐시 문제를 수정한 것은 아니다. 기존 코드의 호환성을 위한 유지 및 사용 중단 표시이며 FindChildDeep 자체에는 Obsolete를 추가하지 않았다.

검증: Unity 재컴파일이 오류 없이 완료됐고, 로드된 assembly에서 클래스·인스턴스 메서드·확장 메서드 모두 ObsoleteAttribute가 있으며 IsError=false임을 확인했다. 메서드 본문은 유지했다. 관련 소스의 git diff --check는 통과했으며 이번 attribute 변경을 위한 새 테스트 추가나 전체 테스트 재실행은 하지 않았다.

## 현재 01·11·15 수정 결과

**01·11·15를 수정했고 Unity 컴파일 및 전체 Play Mode 테스트 54개가 통과했다.**

- **01 / 페이드 CTS 수명:** 교체·OnDisable·OnDestroy는 필드를 먼저 분리하고 취소만 요청한다. 실제 작업을 기다리는 CompleteFadeAsync의 finally에서 CTS를 한 번 해제한다. 해당 CTS가 아직 현재 작업일 때만 필드를 비워 이전 작업의 늦은 완료가 새 페이드를 지우지 않게 했다. 완료한 작업의 해제된 CTS가 필드에 남지 않는다. 완료 정리용 캡처 람다와 ContinueWithAnyway 래퍼도 제거했다. 페이드 시작마다 CTS 및 alpha setter/getter 델리게이트를 만드는 비용은 남아 있다.
- **11 / 타입 캐시:** 목록과 이름 사전을 임시로 완성한 후 하나의 캐시 객체로 교체한다. 준비 여부를 Count가 아닌 캐시 존재 여부로 판단해 빈 결과도 재탐색하지 않는다. ReflectionTypeLoadException에서는 실제 로드된 타입만 사용하고 null 항목을 제외한다. 그 외 탐색 예외에서는 이전 캐시를 유지한다. LINQ·필터용 델리게이트·GetName 객체·ObsoleteAttribute 인스턴스 생성을 제거하고 일반 루프와 IsDefined를 사용했다. 이전에 받은 목록/사전은 갱신 후에도 이전 내용을 유지하며 이후 조회는 새 캐시를 받는다.
- **15 / 종료 콜백:** await만 작업 예외 처리 안에 두고 성공 콜백은 밖에서 실행한다. 성공 콜백의 일반 예외나 취소 예외를 작업 실패/취소로 재분류하지 않아 ContinueWithAnyway의 onEnd가 다시 호출되지 않는다. 콜백 예외는 반환 작업을 통해 호출자에게 전파한다. 원래 작업의 성공·취소·실패 콜백 선택과 실패 로그는 유지했고, 사용자가 수정한 02의 무조건 await 동작도 유지했다. README에 콜백 1회 실행과 예외 전파를 명시했다.

검증: 새 `Assets/Tests/Runtime/CrowRxLifecycleTests.cs`의 16개와 기존 Mathm 34개·TransformExtension 4개를 합친 **Play Mode 54개 통과, 실패·건너뜀 0**을 Pipeline 완료 상태 및 Unity TestResults.xml에서 확인했다. 즉시/비동기 완료 후 재페이드, 페이드 교체, 비활성화 후 재사용, 비활성 컴포넌트의 작업 파괴, FadeInOut 완료 콜백의 재진입, 페이드 실패 정리, 일반/제네릭 종료 콜백 예외의 1회 실행, 원래 작업 결과별 콜백 선택, null 성공 콜백의 await, 빈 타입 캐시, 부분 로드 실패·필터, 갱신 실패 시 이전 캐시 보존을 검사했다. 테스트의 임시 객체와 타입 캐시 상태는 복구했다. 최초 테스트의 2개 실패는 UniTask가 취소 예외 인스턴스를 교체하는데 같은 객체를 기대한 검사였으며, 취소 상태와 1회 호출을 검증하도록 수정했다.

실제 Editor의 전체 타입 탐색 결과는 **타입 2,086개·이름 키 2,086개**였다. 이전 필터로 다시 수집한 결과와 순서까지 일치했고 누락된 이름 키는 0개였다. ProfilerRecorder의 GC.Alloc을 실제 byte[1024] 생성으로 검증한 뒤 준비된 Types/TypesByName을 각각 100회 조회했을 때 **할당 이벤트 0건**이었다. 최초 수집·갱신은 reflection 배열·문자열·새 캐시 컬렉션을 여전히 할당한다. CPU 시간·할당 바이트·Player는 측정하지 않았다.

최종 컴파일 오류는 없으며 Editor는 Play Mode를 종료했다. Console의 예외/경고 각 1개는 NaN duration 실패 정리 및 부분 타입 로드 실패 테스트가 LogAssert.Expect로 검증한 기대 로그다. 기존 로그 기록은 삭제하지 않았다. 관련 소스의 git diff --check도 통과했다.

11의 초기 탐색 자체를 없앤 것은 아니다. 지연 초기화를 유지하며 게임 시작 전에 기존 GatherTypes()를 명시적으로 호출하면 준비 시점을 앞당길 수 있다. API는 메인 스레드 사용을 문서화했다. Unity 6.6 이상은 CurrentAssemblies.GetLoadedAssemblies()로 현재 Unity가 관리하는 어셈블리를 수집해 UAC0005 경고의 AppDomain 경로를 피한다. 더 낮은 버전은 기존 AppDomain 경로를 조건부 컴파일로 유지하며 이번에는 실행 검증하지 않았다. 설치된 Unity 6000.6.5f1의 API 및 [공식 로컬 XML 문서](/D:/Unity/Editor/6000.6.5f1/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.xml:893)에서 새 API의 의미를 확인했다. IL2CPP stripping의 타입 보존과 별도 TypeExtension의 타입 이름 오류는 이번 수정 범위에 포함하지 않았다.

## 이전 일괄 수정 전 남아 있던 항목 (이력)

2026-10-10 현재 **최초 20개 항목 중 13은 Obsolete 사용 중단 처리했고, 나머지 번호 항목은 수정했다.** 아래의 추가 정확성 문제와 후순위 개선 후보는 남아 있다. 상세 분석과 주요 결과 표는 최초 발견 당시의 기록이고, 수정 상태는 이 절 및 상단 수정 결과를 우선한다.

원래 번호와 별개로 남은 정확성 문제:

- `InheritsFrom`의 자기 자신 제외 문제는 수정 완료했다. 동일 타입이면 isIncludeSelf를 그대로 반환하고, 다른 타입은 기존 IsAssignableFrom 판정을 유지한다. Unity 재컴파일 성공 및 Editor eval의 10개 조건에서 동일 타입 포함/제외·기본 오버로드·상속 방향·인터페이스·무관한 타입·null 처리를 확인했다. 이번 변경에서 전체 Play Mode 테스트는 재실행하지 않았다.
- 같은 파일 `:56–86`: 중첩 제네릭 전체 이름을 첫 backtick에서 잘라 선언 타입 이름을 잃을 수 있다. 서로 다른 타입의 표시명이 같아지면 RuntimeTypeCache의 이름 키 충돌로도 이어진다. 표시명과 식별자의 역할을 구분해야 한다.

후순위 성능·GC 개선 후보는 다음과 같다. 구조상 비용이 남아 있다는 판단이며 실제 게임의 병목이나 누수로 확인한 것은 아니다.

- `Mathm.RectIntersect` (`:195–229`): 양 끝점 포함 검사와 bounds Min/Max 4회 순회가 남아 있다. 4점 입력 전제도 명시적 검사·문서화가 없다. 현재 ZLinq 경로를 일반 LINQ 할당으로 단정하지 않는다. `IsArrival` (`:33`)의 방향 정규화도 낮은 우선순위의 산술 비용 후보다.
- `StringExtension.ToUpperFirst` (`:44–58`)와 `GameObjectExtension.GetHierarchyPath` (`:177–224`): 부분 문자열·StringBuilder 및 계층마다 앞에 붙이는 문자열의 중간 할당이 남아 있다. 호출 빈도가 높은 경로부터 확인할 대상이다.
- `MonoBehaviourCrowRx`: 반복 RectTransform 검색과 개별 R3 구독은 상단 수정으로 처리했다. 첫 접근의 취소 상태·등록 및 Awake 이전에만 사용하는 감시 객체의 할당은 남아 있다. 대량 생성 시 초기 비용과 비활성 객체 감시 비용은 별도 측정 대상이다.
- `WhenAny` (`:13,50–57`) 및 `StateTuple`: promise는 호출마다 생성되고 먼저 완료되지 않은 작업은 자동 취소되지 않는다. tuple 풀은 최고 동시 사용량 수준을 보유할 수 있다. 일반적인 WhenAny/풀 수명 특성이므로 자동으로 결함이나 누수라고 판단하지 않는다.
- `SerializedReferenceList<T>` (`:23–26`): 직접 foreach의 boxing은 12 수정으로 해결됐다. IEnumerable 인터페이스를 통한 순회는 여전히 boxing하므로 호출자의 정적 타입을 확인할 대상이다.
- `ComponentExtension.TryGetSingleComponent`와 GameObject의 동일 API: 0개인 정상 부재도 예외 로그를 만드는 경로가 남아 있다. 로그 활성 조건과 호출 계약을 확인한 뒤 개선할 항목이다.
- 에디터의 `DebugUtility.DrawSphere`는 segments 증가 시 다수의 선 그리기와 삼각함수 비용이 남아 있다. 기존 정확성 수정과 별개이며 필요할 때 segments·호출 빈도를 측정할 대상이다.

수정 완료로 분류한 최초 항목은 01–12, 14–20이고, 13은 Obsolete 사용 중단 처리다. 11의 최초 수집, 12의 인터페이스 순회나 19·20 주변의 후순위 최적화까지 모두 무할당으로 만든다는 의미는 아니다. 남은 정확성 항목은 TypeExtension의 중첩 제네릭 이름 처리이며, 후순위 성능 개선은 실제 호출 빈도와 측정을 기준으로 선택할 수 있다.

## 현재 09·18 확인 및 19·20 수정 결과

**사용자의 09·18 수정은 정상 동작을 확인했고, `Mathm.cs`의 19·20을 수정했다. Unity 컴파일 및 Play Mode 회귀 테스트 34개가 통과했다.** 이전 확인 기록은 아래에 별도로 남겼다.

- **09:** 두 RunSafeAsync 모두 try 내부에서 작업 함수 호출 전에 `token.ThrowIfCancellationRequested()`를 실행한다. 사전 취소된 token에서 일반·제네릭 작업 함수 호출이 모두 0회이고, 정상 token은 두 함수에 그대로 전달되는 것을 테스트했다. RunSafe.cs의 작업 시작 전후 SHA-256이 일치했다. 취소는 작업 내부에서도 token을 사용하는 협력적 계약이다.
- **18:** 반환하는 Matrix4x4의 원소를 올바르게 보간한다. 원본은 유지하고, 중간값 및 t=0/1 결과를 테스트했다. 이 방식의 호출자는 `matrix = matrix.Lerp(target, t);`처럼 결과를 대입해야 한다. 사용자가 수정한 Lerp 본문은 유지했다.
- **19 / 교차:** XY 선분의 끝점 접촉, 공선 중첩·분리, 0 길이 선분을 처리한다. 중첩의 반환점은 source1에서 source2 방향으로 처음 만나는 점이며 z=0이다. 계산 중간값은 double을 사용한다. Rect/RectIntersect의 공선 오답 경로도 같은 선분 판정으로 수정했고 Rect.Intersect의 Vector2[4] 할당은 지역 값 4개로 교체했다. RectIntersect의 기존 4점 입력 전제와 bounds 순회 방식은 이번에 바꾸지 않았다.
- **19 / 포함:** Collider 중심과 접촉 방향의 각도 대신 실제 ClosestPoint와 질의점의 거리로 내부·경계를 판정한다. 허용오차는 world 거리 0.00001이다. 활성 Box/Sphere/Capsule 및 mesh가 있는 convex MeshCollider를 지원하고, null·비활성·비지원 대상은 false를 반환한다. 기존 useRigidbody 인수는 호환성을 위해 남겼지만 판정에는 영향을 주지 않는다. Rigidbody 무게중심이 collider 바깥에 있어도 실제 collider 형상만 검사한다.
- **20 / 감쇠:** strength=0에서 속도를 유지하고 기존 단위의 이동량을 반환한다. 기존 ms 반올림과 0.06 계수를 유지하며, 작은 강도는 안정적인 double 계산과 급수로 처리한다. 유한한 strength는 [0,1000], deltaTime은 [0,1]로 제한한다. 최대 강도는 양의 시간에서 즉시 감쇠하고, 0 시간은 속도를 유지한다. NaN/Infinity 강도·시간은 ArgumentOutOfRangeException이다. float/Vector2/Vector3에 같은 계산을 적용한다.
- **20 / 비동기 보간:** 사전 취소는 getter/setter를 호출하지 않는다. 사용자 요청으로 getter/setter 전후 및 Yield 직전의 중복 검사와 SetLerpValue 헬퍼를 제거했고, 시작 시 ThrowIfCancellationRequested와 UniTask.Yield(token)만 남겼다. callback에서 취소하면 다음 Yield에서 취소로 종료하며, 즉시 완료나 마지막 setter 안의 취소는 완료 결과를 바꾸지 않는다. 진행률은 [0,1]로 제한하고, 정상 시간 완료 시 곡선 끝값과 관계없이 endValue에 도달한다. 곡선은 중간 경로에 적용한다. duration<=0의 즉시 완료, reset=false의 남은 거리 비례 시간, scaled/unscaled 시간 선택은 유지한다. NaN/Infinity duration은 값 쓰기 전에 예외로 종료한다.

ClosestPoint의 내부/경계 반환, 지원 collider 및 비활성 시 입력점 반환 의미는 공식 API와 대조했다. 비활성 대상에 대한 guard는 모든 점을 내부로 판정하는 오답을 막는다. [Unity Collider.ClosestPoint](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Collider.ClosestPoint.html), [Unity Physics.ClosestPoint](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics.ClosestPoint.html)

검증: `Assets/Tests/Runtime/MathmTests.cs`의 **Play Mode 34개 통과, 실패·건너뜀 0**을 Pipeline 완료 상태와 Unity `TestResults.xml`에서 확인했다. 15개의 교차 사례, Rect 경계, 이동·회전·비균일 scale·center offset·Rigidbody 무게중심·비활성 및 convex mesh 포함 판정, 감쇠의 0/작은/최대/범위 밖 입력, 사전 취소·callback 취소의 다음 Yield 처리·Yield 취소·즉시 완료 및 마지막 setter 취소 시 정상 완료, 정상 끝점, timeScale=0에서 scaled/unscaled 동작을 포함했다. 임시 객체/mesh는 제거하고 Time.timeScale은 원래 값으로 복구했다. 테스트 assembly에 UniTask 참조를 추가했다. 컴파일 오류 배열은 비어 있고 최종 Console 현재 오류 수는 0이다.

취소 검증 주의: 설치된 UniTask의 `AsTask()`는 OperationCanceledException을 Faulted Task로 변환했다. 원래 UniTask가 Canceled임을 별도로 확인하고, 회귀 테스트에서는 일반 async Task 함수로 UniTask를 실제 await해 취소 결과를 관찰했다. 이 변환 라이브러리는 수정하지 않았다.

GC: Editor eval에서 Recorder를 실제 배열 생성으로 검증하고 워밍업 후 Rect.Intersect, GetIntersectPosition, float SpringDampen을 각각 100회 호출했을 때 **GC.Alloc 이벤트는 각각 0건**이었다. 객체 생성·async 경로·콜백의 할당을 포함한 결과가 아니며 CPU 시간, 바이트 수, Player 측정은 수행하지 않았다. `git diff --check`도 통과했다.

## 이전 사용자 수정 09·12 확인과 18 설명 (09 사전 검사·18 반환 적용 전)

현재 소스와 Git diff를 대조하고, Pipeline eval로 로드된 Unity 코드의 동작을 확인했다. 이번 확인에서는 런타임 소스를 변경하지 않았다.

**09: 토큰 전달과 외부 취소 래퍼 제거는 올바르게 수정됐다.** `Func<CancellationToken, UniTask>` 및 제네릭 버전이 전달받은 token으로 `taskFunc(token)`을 호출한다. 두 버전 모두 같은 token이 전달되는 것을 실행으로 확인했다. `AttachExternalCancellation`도 제거되어 해당 래퍼·취소 등록 경로가 없어졌다. RunSafe 전체가 무할당이라는 뜻은 아니며, 호출자의 캡처 람다 및 비동기 작업 자체 비용은 별개다.

남은 조건은 두 가지다. 작업 함수가 받은 token을 실제 비동기 API에 전달하거나 직접 검사해야 협력적으로 취소된다. token을 무시한 작업은 취소 후에도 Pending임을 확인했다. 또한 **이미 취소된 token이어도 현재 코드는 작업 함수를 호출한다.** 취소된 호출이 동기적인 부작용조차 시작하지 않아야 한다면 두 `RunSafeAsync`의 try 안에서 `taskFunc(token)` 호출 전에 `token.ThrowIfCancellationRequested();`를 추가하는 것이 좋다. 이 검사 추가는 검토 제안이며 이번에 적용하지 않았다.

공개 인수가 `Func<UniTask>`에서 토큰을 받는 Func로 바뀌었으므로 기존 외부 호출자는 아래처럼 맞춰야 한다. 이 프로젝트의 자체 소스에는 별도의 RunSafe 호출자가 발견되지 않았다.

```csharp
CrowTask.RunSafe(ct => DoWorkAsync(ct), token);
```

**12: 권장한 방식대로 올바르게 수정됐다.** 공개 `GetEnumerator()`는 `List<T>.Enumerator`를 반환하고, 두 IEnumerable 구현은 명시적으로 분리됐다. 따라서 변수의 정적 타입이 `SerializedReferenceList<T>`일 때 foreach는 struct enumerator를 직접 사용한다. `IEnumerable<T>`나 비제네릭 `IEnumerable`로 받아 순회하면 인터페이스 반환 과정의 boxing이 남는다. `Items`도 `IReadOnlyList<T>`로 노출하므로 그 인터페이스의 foreach까지 무할당이라고 해석하면 안 된다.

Unity `GC.Alloc` Recorder를 실제 `new byte[1024]`로 검증한 뒤 문자열 항목 3개를 워밍업 후 100회씩 순회했다. 직접 순회는 **할당 이벤트 0건**, `IEnumerable<string>` 순회는 **100건**, 비제네릭 IEnumerable 순회도 **100건**이었다. 세 경로 모두 같은 값·개수를 읽었다. 문자열을 사용해 항목 값 자체의 boxing 비용과 enumerator 비용을 구분했다. Editor eval의 작은 조건별 측정이며 Player 전체 성능이나 모든 소비자의 할당을 보장하지 않는다.

**18: 행렬을 계산하지만 호출자의 원본은 바뀌지 않는 문제다.** 현재 코드는 다음과 같다.

```csharp
public static void Lerp(this Matrix4x4 org, Matrix4x4 target, float t)
    => Matrix4X4Lerp(ref org, org, target, t);
```

`Matrix4x4`는 struct이고 `this Matrix4x4 org`는 값으로 받는다. 호출 시 원본 행렬이 매개변수 org로 복사된다. 내부의 `ref org`는 이 **지역 복사본**을 가리키므로 원본까지 수정하지 않는다. 반환형도 void라서 계산한 복사본을 호출자에게 전달하지 않는다. 다음 예시를 실제 Unity에서 확인했다.

```csharp
var matrix = Matrix4x4.identity; // m00 = 1
var target = Matrix4x4.Scale(new Vector3(2, 3, 4)); // m00 = 2
matrix.Lerp(target, 0.5f);
Debug.Log(matrix.m00); // 실제 결과: 1. 기대한 중간값 1.5가 반영되지 않음

var result = default(Matrix4x4);
Mathm.Matrix4X4Lerp(ref result, matrix, target, 0.5f);
Debug.Log(result.m00); // 실제 결과: 1.5
```

즉 `Matrix4X4Lerp(ref result, ...)`의 계산은 작동하고, 그 함수를 감싼 void 확장 메서드가 결과를 버린다. 계약에 따라 다음 중 하나로 바꿀 수 있다. 아래는 설명용 제안이며 18은 수정하지 않았다.

```csharp
// 결과를 반환하는 방식: 호출자는 matrix = matrix.Lerp(target, t)로 사용
public static Matrix4x4 Lerp(this Matrix4x4 org, Matrix4x4 target, float t)
{
    Matrix4x4 result = default;
    Matrix4X4Lerp(ref result, org, target, t);
    return result;
}

// 또는 원본을 직접 수정하는 방식: matrix.Lerp(target, t)
public static void Lerp(this ref Matrix4x4 org, Matrix4x4 target, float t)
    => Matrix4X4Lerp(ref org, org, target, t);
```

두 선언은 대안이다. 반환 방식은 호출자가 결과를 대입해야 하고, ref 방식은 수정 가능한 행렬 변수에서 호출해야 한다. 현재 자체 소스에는 해당 행렬 확장 메서드의 호출자가 발견되지 않았지만, 외부 소비자의 기대 계약은 별도로 확인해야 한다. 이는 GC보다 **결과가 사라지는 기능 오류**다.

## 런타임 06·07·08 수정 결과

`Assets/CrowRx/Runtime/TransformExtension.cs`의 06·07·08을 수정했다. 사용자가 수정한 02는 현재 `T result = await task;` 후 성공 콜백을 호출하는 코드임을 확인했으며, 이 작업에서는 수정하지 않았다. 다른 런타임 발견 사항은 그대로 남는다.

| ID | 변경 내용 | 유지한 동작 |
|---|---|---|
| 06 | 노드마다 `GetComponents<T>()` 배열을 만드는 재귀 수집을 제거했다. `ListPool<T>`의 임시 리스트에 `GetComponentsInChildren(true, gathered)`로 수집하고, 호출자의 리스트 끝에 추가한다. 임시 리스트는 using으로 항상 반환한다. | 자기 자신·비활성 자손 포함, 기존 리스트 내용과 깊이 우선 순서 유지. 반환값은 추가 후 전체 리스트가 비어 있지 않은지 판정한다. |
| 07 | 조상/자손마다 전체 계층 검색을 다시 실행하는 경로를 제거했다. IncludeThis 상위 검색은 엔진 검색 1회 후 실패 시 조상별 직접 검사, 자기 자신 제외 상위 검색은 엔진 검색 최대 2회로 종료한다. 하위 검색은 엔진 검색 1회 후 실패 시 부모/형제 링크로 전위 순회하며 재귀 스택·임시 컬렉션을 사용하지 않는다. | 활성·비활성 우선순위와 자기 자신 포함/제외의 기존 선택 결과를 유지했다. 깊은 체인의 실패 경로는 O(d²)에서 O(d), 하위 전체 실패 경로는 O(N)으로 줄었다. 비활성 처리 규칙을 모든 API에서 동일하게 바꾸지는 않았다. |
| 08 | 자식 참조 복사와 Destroy를 try로 감싸고, finally에서 `ArrayPool<GameObject>.Shared.Return(..., clearArray: true)`로 반환한다. | 기존 `Object.Destroy`의 프레임 끝 삭제 동작 유지. 사용하지 않은 배열 여유 공간까지 참조를 지운다. |

검증은 설치된 Pipeline으로 이 프로젝트의 Unity `6000.6.5f1`에서 수행했다.

- Unity 컴파일 완료: `failed=false`, `compilationFailed=false`, 컴파일 오류 배열 비어 있음.
- `TransformExtensionTests` **Play Mode 테스트 4개 통과, 실패·건너뜀 0**. Pipeline 완료 결과와 Unity의 `TestResults.xml`을 확인했다.
- 수집: 여러 컴포넌트, 비활성 가지, 기존 결과에 append, 깊이 우선 순서, 결과가 추가되지 않은 경우의 bool 계약을 확인했다.
- 검색: 5개 노드의 활성 여부 32가지 × 컴포넌트 배치 32가지 = **1,024가지 조합**에서 각 노드의 검색 3종을 이전 구현과 대조했다(15,360회 비교). 추가로 256단계 체인의 부재 검색을 확인했다.
- 삭제: 프레임 후 자식 제거와 같은 풀 버퍼의 재사용을 확인했다. `ReferenceEquals(slot, null)`로 사용 슬롯·여유 슬롯 모두 실제 managed 참조가 지워졌는지 검사했다. 예외 경로의 반환은 finally 구조로 보장하며, 파괴 중 예외를 강제로 주입한 테스트는 수행하지 않았다.
- `git diff --check` 통과. `FindChildDeep`, `SetActiveAllChildren`, `SetLocalTransformDefault`의 본문은 유지했다. 회귀 테스트는 `Assets/Tests/Runtime/TransformExtensionTests.cs`, 테스트 assembly는 `CrowRx.Runtime.Tests.asmdef`에 추가했다. 테스트 fixture는 종료 시 제거된다.

GC 비교 결과: 5개 노드에 Collider 1개씩, 충분한 결과 리스트 용량, 풀 워밍업 후 수집 **100회**를 같은 스레드에서 Unity `GC.Alloc` Recorder로 측정했다. 이전 배열 수집은 **할당 이벤트 500건**, 수정된 수집은 **0건**이었다. 먼저 `new byte[1024]`로 Recorder가 실제 할당을 감지하는지 확인했다. 이는 테스트 조건의 이벤트 수이며 바이트 수나 실제 게임 프레임의 GC 감소량이 아니다. 최초 리스트 생성·용량 확장에는 할당이 남고, 풀은 리스트 용량을 보유한다. CPU 시간과 별도 Player 실행은 측정하지 않았다. 이 Unity 런타임의 `GC.GetAllocatedBytesForCurrentThread()`는 실제 배열 생성에도 0을 반환하여 측정 근거에서 제외했다.

검증 중간에 Unity 자체 `Unity.PlayMode.Editor.DefaultScenario`의 종료 상태 assertion이 한 번 기록됐다. 최종 실행에서는 테스트 4개가 통과했고 Console의 현재 오류 수는 0이었다. 이 기록과 기존 `RuntimeTypeCache`의 `UAC0005` 경고를 이번 코드 수정으로 해결한 것으로 취급하지 않는다.

## 에디터 수정 결과

**에디터 코드 8개 파일의 발견 사항을 수정했고, 설치된 Pipeline으로 실제 Unity 컴파일 및 EditMode 테스트 3개 통과를 확인했다.** 이 에디터 수정 단계에는 런타임을 포함하지 않았으며, 이후의 런타임 수정은 위에 별도로 기록했다. 아래의 최초 분석은 수정 전 기록이며, 그 부분의 줄 번호도 수정 전 기준이다.

| 수정 파일 (`Assets/CrowRx/Editor/` 기준) | 변경 내용 |
|---|---|
| `EditorCrowRx.cs` | 무조건 Repaint 제거. `ContinuousRepaint`를 재정의하는 인스펙터만 지속 갱신. 진입·종료 콜백이 예외를 던져도 이벤트 구독을 해제한다. 기존 인스턴스별 수명 관리 변경을 유지했다. |
| `EditorCrowRxExtension.cs` | 단순 필드용 캡처 람다 제거. 값 타입 scope로 부모의 GUI 색상·활성 상태를 보존하고 layout/scroll을 finally에서 닫는다. 폭 옵션은 최대 32개 캐시하고 고정 옵션 배열을 재사용한다. UI Toolkit 기본 필드 추가의 불필요한 ZLinq 의존성을 제거했다. |
| `EditorWindowCrowRx.cs` | 기본 지속 Repaint 제거, 선택적 Inspector 갱신 주기 제공. 여러 창의 Instance 복구와 예외 시 구독 해제 보강. 직접 필드 그리기, 메타데이터·타입 해석·스타일 캐시. struct 변경 반환, 구체적인 enum 및 상속 private 필드 편집, 빈 Popup 처리, Type 배열을 받는 Popup 제공. Outline 스타일 색상 복구와 Label 글자 크기도 수정했다. |
| `SerializeReferenceListDrawer.cs` | 중첩·배열·List·상속 private·다형적 managed reference 필드 경로 해석. SerializedObject별 목록 캐시 교체 및 최대 32개 제한, 최초 그리기 전 높이 계산. 타입 후보·GUIContent 캐시. 생성 가능한 직렬화 타입만 제시하고, 메뉴 콜백은 대상과 경로를 보관해 새로운 SerializedObject로 수정한다. 생성자 실패 시 빈 항목을 남기지 않으며 다중 선택의 추가는 비활성화한다. |
| `ComponentExtension.cs` | 필드·속성 메타데이터와 부재 결과 캐시, 상속 private 멤버 지원, enum 비교 boxing 제거. indexer와 getter 없는 속성을 제외하고 null/파괴된 컴포넌트는 기본값을 반환한다. |
| `DebugUtility.cs` | 원의 인접 점 중복 계산 제거, 잘못된 반지름·segment 입력 방어. 이동된 Box 연결선, XZ/YZ Arc 축, 역순 Rectangle 모서리 수정. |
| `GizmosUtility.cs` | 아래 방향·임의 길이 normal의 Plane 축/크기 수정. CapsuleCollider 방향과 짧은 높이 처리, null collider 방어. 기존 Gizmos.matrix 적용 계약 유지. |
| `PrefabInformation.cs` | 일반 객체의 빈 프리팹 경로를 null로 정규화. 프리팹 asset·instance·stage와 scene 객체 구분 보강. 명시적인 `Refresh()`로 계층 변경 후 스냅샷을 갱신할 수 있다. |

연결된 Editor는 `6000.6.5f1`, 프로젝트 경로는 `D:/Project/personal/crowlib/crowrx/src/CrowRx.Unity`다. 다른 프로젝트의 IDE 검사 결과를 검증 근거로 사용하지 않았다.

검증 결과:

- Unity `recompile_status`: `completed`, `failed=false`, `compilationFailed=false`, 오류 배열 비어 있음.
- `CrowRx.Editor.Tests` EditMode 테스트: **3개 통과, 실패 0, 건너뜀 0**. Pipeline 완료 상태와 Unity가 저장한 `C:/Users/newho/AppData/LocalLow/DefaultCompany/CrowRx_Package_Unity/TestResults.xml`을 모두 확인했다.
- GUI/직렬화 테스트의 32개 검증: 실제 EditorWindow Layout 이벤트 안에서 부모 GUI 상태와 예외 복구, struct·enum 변경 반환, 상속 필드, 빈 Popup, 여러 창의 수명·구독 정리, 중첩 필드 경로, 목록 높이·소유자 교체·생성 후보·추가 콜백을 확인했다. 필드 변경값은 테스트용 override로 공급했으며 실제 키보드 입력이나 메뉴 클릭을 검증한 것은 아니다.
- 추가 테스트: 타입별 편집 메타데이터 및 폭 옵션 재사용·캐시 상한, reflection 필드/속성 조회와 부재 처리, 일반 객체·프리팹 asset·instance·unpack 후 Refresh 판정. 임시 preview scene과 프리팹 fixture는 테스트 종료 후 제거됐다. 사용자 장면은 저장하지 않았다.
- `git diff --check` 통과. 수정 시작 전후 런타임 폴더 47개 파일(소스와 meta)의 SHA-256이 일치했다. 기존 런타임·Core 변경을 보존했으며, Pipeline 설치에 따른 패키지 설정 변경도 유지했다.

테스트 소스는 `Assets/Tests/Editor/CrowRxEditorRegressionTests.cs`, 에디터 전용 테스트 assembly는 `Assets/Tests/Editor/CrowRx.Editor.Tests.asmdef`에 추가했다.

성능 검증의 한계: 불필요한 그리기·캡처·메타데이터 검색을 줄인 것은 코드로 확인했지만, **Profiler GC.Alloc 및 전후 CPU 시간은 측정하지 않았다.** Unity IMGUI 자체 할당, reflection 값 boxing, SerializedProperty wrapper, 메뉴를 열 때의 콜백 생성은 남는다. DrawSphere의 선 그리기 호출 수는 여전히 O(segments²)이며 실제 소비자의 segments/호출 빈도에 따라 비용이 달라진다. 디버그 도형 수정은 수식·소스와 컴파일로 검증했으며 SceneView 화면 비교는 수행하지 않았다. 버퍼에는 런타임 `RuntimeTypeCache.cs`의 기존 `UAC0005` 경고 기록이 있었고 이번 에디터 수정으로 해결한 것으로 취급하지 않는다.

갱신 정책 변경에 따른 사용법: 실시간 표시가 필요한 파생 인스펙터/창에서는 `protected override bool ContinuousRepaint => true;`를 지정하거나 데이터 변경 이벤트에서 `Repaint()`를 호출한다. 창의 opt-in 갱신은 `OnInspectorUpdate` 주기다. `PrefabInformation`은 스냅샷이므로 unpack·계층 변경 후 `Refresh()`를 호출한다.

## 최초 정적 검토 (수정 전 기록)

**우선순위가 높은 부분은 페이드 CTS 수명 오류, 작업을 기다리지 않는 제네릭 continuation, 에디터의 지속적인 Repaint, 계층 검색의 반복 탐색과 배열 할당이다.** 일부는 정상적인 호출만으로 잘못된 결과를 만드는 코드 결함이고, 일부는 자주 호출할 때 비용이 커지는 구조다. 실제 프레임 시간이나 GC.Alloc 수치는 측정하지 않았다.

## 범위와 근거

- `Assets` 아래 직접 포함된 C# **36개 파일, 3,955줄** 전체를 읽었다. 런타임 20개, 에디터 16개이며 주석과 빈 줄을 포함한 줄 수다. 자체 shader/HLSL/compute 소스는 발견되지 않았다.
- Unity 설정은 `6000.6.5f1`, UPM 패키지는 CrowRx `1.0.10`, 설치된 Core는 `1.0.11`이다. 설치된 UniTask `2.5.11`, R3 Unity `1.3.1` 소스에서 취소 래퍼와 트리거 동작을 대조했다.
- `CrowRx.csproj`에는 `USING_ZLINQ`가 있고 `USING_ZSTRING`은 없다. 따라서 현재 `Mathm.RectIntersect`는 ZLinq 경로, `ToUpperFirst`는 일반 StringBuilder 경로다. 생성된 프로젝트 파일은 근거로 읽기만 했다.
- 제3자 패키지 전체와 Core DLL 전체를 별도의 라이브러리 감사 대상으로 검토한 것은 아니다. 아래 결과는 이 Unity 프로젝트의 자체 코드 전체와 관련된 의존성 구현에 대한 검토다.
- 자체 게임 스크립트·테스트 스크립트·추가 prefab은 발견되지 않았다. `SampleScene.unity`에서 CanvasGroupFade, TriggerEventListener, FindObjectHelper의 GUID 참조도 발견되지 않았다. 에디터 베이스 클래스의 구체적인 사용자 구현도 이 `Assets`에는 없다. 따라서 발견한 비용을 현재 게임에서 이미 발생 중인 병목으로 해석하면 안 된다. 런타임 생성과 외부 소비 프로젝트는 이 조사로 배제할 수 없다.
- Unity 실행, 빌드, 테스트, Profiler 및 Player 측정은 수행하지 않았다. 아래 예시는 소스 흐름과 수식으로 도출한 발생 조건이며 실행 재현 결과가 아니다.
- 소스와 설정을 수정하지 않았다. 이 보고서만 추가했다. 기존의 변경·삭제·미추적 파일은 유지했다.

아래 경로는 별도 표시가 없으면 `Assets/CrowRx/` 기준이다. P1은 정상 사용을 깨는 우선 수정 대상, P2는 반복 비용 또는 특정 조건의 기능 오류, P3은 호출 빈도와 계약을 먼저 확인할 개선 후보다. **우선순위는 측정된 비용 순위가 아니다.**

## 주요 결과

| ID | 우선순위 | 위치 | 문제와 발생 조건 |
|---|---|---|---|
| 01 | P1 | `Runtime/Helper/CanvasGroupFade.cs:32,40,48,56` | 완료한 CTS를 필드에 남겨 다음 Fade 호출이 해제된 CTS에 Cancel을 호출한다. |
| 02 | P1 | `Runtime/Tasks/ContinueWithAnyway.cs:57` | 제네릭 ContinueWith의 성공 콜백이 null이면 await 자체가 실행되지 않는다. |
| 03 | P2 | `Editor/EditorCrowRx.cs:24`, `Editor/EditorWindowCrowRx.cs:50` | 입력이나 변경이 없어도 다음 Repaint를 요청한다. |
| 04 | P2 | `Editor/EditorCrowRxExtension.cs:15–106`, `Editor/EditorWindowCrowRx.cs:111–220` | 단순 GUI 필드마다 캡처 클로저와 델리게이트를 생성한다. |
| 05 | P2 | `Editor/EditorWindowCrowRx.cs:450–494` | GUI마다 reflection/LINQ/attribute 검색과 값 타입 boxing. struct 변경도 원본에 반영되지 않는다. |
| 06 | P2 | `Runtime/TransformExtension.cs:141–151` | 리스트를 받으면서 각 노드에서 배열 반환 GetComponents를 호출한다. |
| 07 | P2 | `Runtime/TransformExtension.cs:29–83,155–169` | 실패한 상위·하위 검색을 조상/자손마다 다시 실행한다. 깊은 체인의 실패 경로는 O(d²). |
| 08 | P2 | `Runtime/TransformExtension.cs:96–108` | GameObject 배열을 참조 제거 없이 풀에 반환하고 finally도 없다. |
| 09 | P2 | `Runtime/Tasks/RunSafe.cs:17,36` | token은 원래 작업에 전달되지 않는다. 외부 취소 래퍼의 할당과 작업 지속이 발생한다. |
| 10 | P2 | `Runtime/GameObjectExtension.cs:26–53` | 이벤트 전달마다 이벤트 데이터·리스트 생성과 전체 raycast. 반환값과 입력 좌표도 부정확하다. |
| 11 | P2 | `Runtime/RuntimeTypeCache.cs:75–124` | 최초 접근에서 전체 assembly/type reflection과 문자열 생성. 실패 시 부분 캐시가 노출될 수 있다. |
| 12 | P2 | `Runtime/SerializeReferenceListAttribute.cs:24–25` | List의 struct enumerator를 IEnumerator로 반환하여 순회마다 boxing한다. |
| 13 | P2 | `Runtime/Helper/FindObjectHelper.cs:10–27` | 깊은 자손 변경·이름 변경·파괴에 캐시가 낡고, 실패 검색은 계속 반복한다. |
| 14 | P2 | `Editor/SerializeReferenceListDrawer.cs:25–80,108–111` | 중첩 필드 타입 해석 실패와 캡처된 SerializedProperty의 수명·캐시 식별 문제. |
| 15 | P2 | `Runtime/Tasks/ContinueWithAnyway.cs:33–68` | 성공 콜백 예외를 작업 실패로 처리하여 Anyway 종료 콜백이 다시 실행될 수 있다. |
| 16 | P2 | `Editor/EditorCrowRxExtension.cs:108–238` | GUI 상태를 이전 값 대신 true/white로 복구하고 예외 시 복구하지 않는다. |
| 17 | P2 | `Editor/EditorCrowRx.cs:63`, `Editor/EditorWindowCrowRx.cs:55` | 사용자 종료 콜백이 예외를 던지면 전역 이벤트 구독 해제가 생략된다. |
| 18 | P2 | `Runtime/Mathm.cs:46` | Matrix4x4 확장 Lerp가 복사본만 수정하고 결과를 반환하지 않는다. |
| 19 | P2 | `Runtime/Mathm.cs:86–129,419–444` | 분리된 공선 선분 교차와 이동된 Collider 포함 판정에 오답 경로가 있다. |
| 20 | P2 | `Runtime/Mathm.cs:231–281,284–416` | SpringDampen의 0 강도에서 NaN, LerpAsync의 사전 취소에서도 setter 실행. |

## 상세 분석

### 01. 페이드가 끝난 뒤 다음 페이드가 실패할 수 있다

`Fade`와 `FadeInOut`은 `_ctsFade`를 만들고 `currentCts`로 복사한다. 완료 콜백은 `currentCts.Release()` 후 **지역 변수**만 null로 만든다. `_ctsFade`에는 해제된 인스턴스가 남는다.

발생 흐름은 `Fade → 완료 → CTS Cancel/Dispose → 다음 Fade → _ctsFade?.Cancel()`이다. null 조건 검사는 해제 여부를 검사하지 않으므로 다음 호출은 ObjectDisposedException 경로에 들어간다. 짧은 duration으로 동기 완료되는 경우에도 같은 문제가 생긴다. 해제된 CTS의 Cancel은 공식 API에서도 예외로 명시되어 있다. [CancellationTokenSource.Cancel](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.cancel)

개선 방향: CTS를 소유하고 해제하는 경로를 하나로 정하고, 완료한 인스턴스가 아직 현재 `_ctsFade`인 경우에만 필드도 null로 만든다. 이전 작업의 늦은 완료가 새 CTS를 지우지 않도록 인스턴스 비교가 필요하다. OnDisable은 취소를 요청하고, 실제 작업의 finally가 해제를 담당하는 방식도 검토할 수 있다.

GC 관점에서는 페이드 시작마다 CTS 1개와 정리용 캡처 클로저가 생성된다. FadeAsync의 setter/getter도 인스턴스를 캡처하는 델리게이트다. 이들은 **시작 시 비용**이며 매 프레임 생성되는 클로저는 아니다. CTS 수명 오류를 해결한 후 전환 빈도가 높은 UI에서 델리게이트 캐시 등을 검토하면 된다.

### 02. 제네릭 ContinueWith가 작업을 기다리지 않는다

`ContinueWith<T>`의 핵심 줄은 `onSuccess?.Invoke(await task)`다. 성공 콜백이 null이면 호출 인수인 `await task`도 평가하지 않는다. null 조건 연산자의 단락 동작은 공식 C# 문서와 일치한다. [C# null 조건 연산자](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/member-access-operators)

따라서 `pendingTask.ContinueWith<int>(null, onCanceled, onException)`은 작업을 기다리지 않고 성공 완료할 수 있다. 취소/예외 관찰도 하지 않아 작업 완료·자원 반환 흐름이 깨진다.

개선 방향: 먼저 `T result = await task;`로 반드시 소비하고, 이후 `onSuccess?.Invoke(result)`를 실행한다. 여기서 문제는 공개 3콜백 오버로드다. 현재 Anyway<T> 등은 null인 onEnd도 별도의 람다로 감싸므로 그 경로를 같은 버그라고 단정하지 않는다.

### 03–04. 지속적인 Repaint와 GUI 헬퍼 할당이 결합한다

`OnInspectorGUI`와 `OnGUI` 끝에서 조건 없이 Repaint를 호출한다. 눈에 보이는 값이 바뀌지 않아도 추가 GUI 이벤트가 요청되고, 그때마다 각종 필드 헬퍼가 다시 실행된다. 실제 이벤트 발생률과 CPU 비용은 Unity 스케줄링과 열린 창 수에 따라 달라진다.

FieldObject, FieldText, Toggle, Label 등은 단순 값 표시를 위해 인수를 캡처하는 람다를 만든다. 창의 FieldInt/FieldFloat 등은 BaseField 내부의 추가 캡처까지 거친다. 일반적인 컴파일 결과에서 캡처 객체와 델리게이트가 생기며, GUILayout.Width/ExpandWidth 호출은 옵션 객체와 params 배열 비용도 추가한다. 현재 인스펙터의 Script 필드도 매 GUI 호출 이 경로를 사용한다.

개선 방향: 기본 베이스 클래스는 사용자 입력·데이터 변경 시만 다시 그리도록 하고, 계속 갱신해야 하는 파생 창에만 별도 정책을 제공한다. 단순 GUI 필드 헬퍼는 직접 필드를 그리거나 scope 기반으로 상태를 관리한다. 콘텐츠 콜백이 필요한 컨테이너 API까지 일괄 제거할 이유는 없다. 캐시할 수 있는 GUIContent/GUILayoutOption은 필요할 때 캐시한다.

**이 코드는 Editor 전용 asmdef에 있어 Player의 프레임 GC 문제와는 구분해야 한다.** 에디터 Play Mode에서 함께 측정하면 결과를 혼동할 수 있다.

### 05. PropertyEditViewer는 비용과 기능 오류가 함께 있다

각 호출에서 GetFields 배열, LINQ 필터, GetCustomAttributes와 First, GetValue/SetValue, dynamic 호출을 사용한다. 그리기 이벤트마다 필드 수에 비례한 reflection·boxing 비용이 생긴다. dynamic 바인더 초기 비용과 반복 비용은 구분해야 하며, 모든 바인더 호출이 새 객체를 만든다고 단정하지 않는다.

값 타입 `STRUCT`를 전달하면 `SetValue(data, ...)`가 박싱된 복사본을 수정하고 반환하는 `data`는 그대로다. 예를 들어 사용자 struct의 int 필드를 GUI에서 바꾸어도 반환 struct에 변경이 반영되지 않는 경로다. 또한 실제 enum 필드의 FieldType은 구체적인 enum 타입이므로 `fieldType.Equals(typeof(Enum))` 분기가 실행되지 않는다.

개선 방향: 타입별로 FieldInfo·속성·표시명을 캐시한다. struct 지원을 유지한다면 하나의 박싱 객체에 모든 변경을 적용하고 마지막에 언박싱해서 반환한다. enum은 IsEnum으로 판별한다. GUI 렌더링에서 dynamic과 범용 변환이 꼭 필요한지 재검토한다.

### 06. 계층 컴포넌트 수집은 각 노드마다 결과 배열을 요청한다

외부 API가 List를 받지만 내부 GatherComponent는 매 노드에서 `root.GetComponents<T>()`를 호출한다. 결과가 있는 노드에서는 배열이 생성되고 AddRange가 수행된다. 결과가 없는 경우 빈 배열을 공유하는지는 엔진 구현에 따라 달라지므로 모든 노드가 반드시 별도 빈 배열을 할당한다고 단정하지 않는다. 리스트 용량이 부족하면 확장 할당도 추가된다.

N개 노드 순회에서 GetComponents 호출 N회가 발생한다. 재사용 리스트를 전달한다고 이 경로 전체가 무할당이 되는 것은 아니다.

개선 방향: `GetComponentsInChildren(includeInactive: true, results)` 또는 풀에서 받은 임시 List에 GetComponents 결과를 담아 한 번 순회한다. **현재 API는 입력 리스트를 Clear하지 않고 결과를 추가한다. Unity의 리스트 오버로드는 기존 내용을 덮어쓴다.** 비활성 포함, 자기 자신 포함, 순서, append 계약을 먼저 결정해야 안전하게 교체할 수 있다. 결과 리스트의 용량 확장은 여전히 생길 수 있다. [Unity GetComponentsInChildren](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Component.GetComponentsInChildren.html)

### 07. 없는 컴포넌트를 찾는 경로에서 중복 탐색이 커진다

FindUpwardIncludeThis는 각 단계마다 TryGetComponent와 GetComponentInParent를 호출하고, 못 찾으면 부모에서 전체 상위 검색을 다시 한다. FindUpward도 실패 시 조상별로 상위 검색을 반복한다. 깊이 d인 일자 계층에서 실패 검색의 탐색량은 대략 `d + (d-1) + ... + 1`이다.

FindDownwardInternal은 우선 GetComponentInChildren으로 하위 전체를 검사한다. 실패하면 자식별로 같은 하위 검사를 재귀적으로 반복한다. 이것도 깊은 일자 계층의 실패 경로가 O(d²)이며, 균형 트리에서는 계층 모양에 따라 달라진다. 정상적으로 첫 검색에서 발견하는 경우를 같은 복잡도라고 부르면 안 된다.

개선 방향: 단일 엔진 검색 또는 노드별 TryGetComponent 1회로 통일한다. 현재 하위 구현은 엔진의 기본 비활성 제외 검색 후 자식에서 직접 TryGetComponent를 하므로 비활성 자손도 최종적으로 찾을 수 있다. 상위 구현은 조건에 따라 비활성 조상의 탐색 의미도 달라질 수 있다. includeInactive와 자기 자신 포함 계약을 확정한 후 변경해야 한다.

FindChildDeep, SetLayerRecursively, ChildCirculateCallback도 재귀이지만 그 사실만으로 GC 결함은 아니다. indexed GetChild 순회에는 foreach enumerator를 생성하는 코드가 없으며, 일반적인 방문 수는 선형이다. 아주 깊은 계층에서는 재귀 스택을 별도로 고려하면 된다.

### 08. 풀에 반환한 배열이 삭제한 객체의 managed wrapper를 붙잡는다

RemoveAllChildren은 ArrayPool에서 GameObject[]를 빌리고 자식 참조를 넣은 뒤 `Return(removalObjects)`로 반환한다. clearArray 기본값이 false이므로 풀에 남는 배열 슬롯이 참조를 유지할 수 있다. Object.Destroy로 native 객체가 파괴되는 것과 managed wrapper의 수집은 별개다. 이를 native GameObject 전체가 영구적으로 살아 있다는 뜻으로 해석하면 안 된다. [ArrayPool.Return](https://learn.microsoft.com/dotnet/api/system.buffers.arraypool-1.return)

중간에 예외가 나면 Return에도 도달하지 않는다. 이때 배열이 풀에서 빌려진 채 영구적으로 뿌리 잡힌다고 단정할 수는 없지만, 재사용 기회가 사라지고 다음 Rent가 새 버퍼를 만들 수 있다.

개선 방향: finally에서 반환하고 참조를 지운다. Shared 풀의 과거 사용자 슬롯까지 고려하면 전체 clearArray:true가 단순한 선택이다. 사용 구간만 지우는 방식은 남은 슬롯이 깨끗하다는 보장이 필요하다. 전체 clear 비용도 자식 수가 많을 때 측정해야 한다.

### 09. RunSafe 취소는 원래 작업의 취소가 아니다

RunSafe의 taskFunc는 인수 없는 Func이므로 전달받은 token을 원래 작업에 전달하지 않는다. 이미 취소된 token이 들어와도 먼저 `taskFunc()`를 실행한다.

설치된 UniTaskExtensions.cs:195–269와 299–327을 확인했다. 취소 가능한 token과 미완료 작업 조합이면 AttachExternalCancellationSource 객체 및 취소 등록이 만들어지고, 내부 RunTask는 원래 작업이 끝날 때까지 기다린다. 외부 취소는 래퍼의 완료 상태만 바꾼다. 원래 작업이 자체적으로 취소를 처리하지 않으면 작업·캡처 상태·래퍼의 수명이 계속된다.

따라서 반복적인 전환·파괴 시 wrapper 할당과 백그라운드 작업 지속이 겹칠 수 있다. 다만 token이 취소 불가능하거나 작업이 이미 완료됐다면 설치된 구현은 원래 task를 그대로 반환한다.

개선 방향: 협력적 취소가 필요한 API에는 Func<CancellationToken, UniTask> 오버로드를 두고 작업 시작 전 취소를 확인한다. 외부 작업을 실제로 취소할 수 없는 경우에는 현재 래퍼의 의미를 문서화한다. 무조건 AttachExternalCancellation을 삭제하면 기존 호출 계약을 바꿀 수 있다.

### 10. 이벤트 전달의 할당과 결과 계약이 불필요하게 비싸다

ExecuteEventExceptThis는 호출마다 PointerEventData와 List<RaycastResult>를 만들고 RaycastAll을 다시 한다. PointerEventData 내부 보유 데이터, 결과 리스트의 확장 및 raycaster 내부 비용도 추가될 수 있다. drag/move처럼 반복 호출되는 상황에서 우선 측정할 대상이다.

`result = resultCount > 1` 때문에 raycast 결과가 2개 이상이면 핸들러를 하나도 실행하지 않아도 true를 반환한다. 또한 첫 번째 다른 결과에서 무조건 break하므로 그 객체·부모가 이벤트를 처리하지 못해도 이후 결과를 검사하지 않는다. 이 두 번째 동작은 '바로 뒤 객체 하나에 전달' 계약이라면 가능하지만 이름만으로는 계약이 명확하지 않다.

입력 eventData가 가진 포인터 좌표를 사용하지 않고 Input.mousePosition으로 새 raycast를 한다. touch/gamepad 포인터와 좌표가 다를 수 있고, Input System 전용 설정에서는 legacy Input 호출의 호환성도 별도 확인이 필요하다. EventSystem.current의 null 검사도 없다.

개선 방향: 포인터 좌표와 true의 의미를 먼저 정의한다. 결과 리스트는 ListPool을 사용하거나 호출자 소유 버퍼를 받는다. PointerEventData 재사용은 중첩 호출과 상태 초기화, EventSystem 변경을 고려해야 하므로 단순 static 공유는 피한다. 상위 이벤트 전달에서 CanHandleEvent와 Execute가 같은 객체를 다시 검사하는 비용은 설치된 uGUI 구현에 따라 대조·측정할 후순위 항목이다.

### 11. RuntimeTypeCache 최초 호출의 스파이크와 부분 캐시 위험

첫 접근에서 GetAssemblies, assembly.GetTypes, GetName, ObsoleteAttribute 검색, LINQ의 Distinct/SelectMany/필터, 타입 이름 문자열 생성, List/Dictionary 확장이 한 번에 실행된다. 필터의 `Any(assemblyName.StartsWith)` 등은 대상 문자열을 받는 델리게이트를 반복해서 생성한다. 한 번만 실행된다면 정상적인 초기화 비용일 수 있지만 첫 게임 동작 시 지연 초기화되면 hitch 후보가 된다.

GetTypes는 실패할 수 있다. `_cachedTypes.Clear()` 이후 AddRange가 열거 중 실패하면 일부 타입이 리스트에 이미 추가될 수 있다. 그 뒤 딕셔너리 생성에는 도달하지 않는다. 다음 TypesByName은 리스트 Count가 0이 아니라는 이유로 GatherTypes를 생략하여 null 또는 이전 딕셔너리를 반환할 수 있다. 공개 GatherTypes 재실행에서도 두 캐시가 어긋날 수 있다.

개선 방향: 임시 컬렉션에 전체 결과를 완성한 후 일관되게 교체한다. 실패한 assembly를 건너뛸지·실패를 전파할지 정책을 정하고 ReflectionTypeLoadException의 유효 Types를 처리한다. 게임플레이 전에 의도적으로 준비하거나 빌드 시 생성된 타입 목록을 사용할 수 있다. IL2CPP stripping 환경에서 이름 기반 reflection이 타입 보존을 보장하지 않는 부분도 별도 Player 검증이 필요하다.

타입 목록을 IReadOnlyList로 노출한다고 내부 List가 불변이 되는 것은 아니다. GatherTypes는 기존 참조가 보는 목록을 변경하며 thread-safe 계약도 없다. Unity 메인 스레드 전용으로 문서화하거나 스냅샷 교체가 필요하다.

### 12. SerializedReferenceList 순회가 boxing한다

`public IEnumerator<T> GetEnumerator() => items.GetEnumerator();`에서 List<T>.Enumerator 값 타입이 인터페이스로 변환된다. 컨테이너를 직접 foreach해도 이 반환 시 boxing이 생긴다. non-generic 경로도 동일한 문제를 가진다.

개선 방향: 공개 pattern 메서드는 `List<T>.Enumerator`를 반환하고 IEnumerable<T>/IEnumerable 구현은 명시적으로 분리한다. 인터페이스를 통한 순회는 여전히 boxing될 수 있으므로 API 개선 후 '모든 순회가 무할당'이라고 표현하면 안 된다.

List에서 컨테이너로 암시적 변환할 때는 새 wrapper를 만들며 필드 initializer의 새 List도 외부 List로 덮어쓴다. 불필요한 빈 List 생성을 없앨 수 있지만 변환이 반복되는 실제 소비자가 있는지 먼저 확인할 P3 항목이다.

### 13. FindObjectHelper 캐시 무효화 범위가 탐색 범위보다 좁다

검색은 깊은 자손까지 하지만 캐시 Clear는 helper가 붙은 Transform의 OnTransformChildrenChanged뿐이다. Unity는 직접 자식의 목록 변경을 이 콜백으로 알린다. 깊은 자손 아래의 추가/삭제/재부모화와 객체 이름 변경까지 이 캐시를 유효하게 유지하지는 못한다. [Unity OnTransformChildrenChanged](https://docs.unity.com/en-us/engine/6000.7/script-reference/unityengine/monobehaviour/ontransformchildrenchanged)

캐시 hit에서 target의 Unity null 여부·이름·소속 계층도 확인하지 않는다. 그러므로 파괴된 대상 wrapper나 계층 밖으로 이동한 객체를 반환할 수 있다. 없는 이름은 캐시하지 않으므로 같은 실패 요청을 반복하면 매번 전체 탐색한다.

개선 방향: 동적 계층에서 캐시를 쓸지 계약을 정한다. 무효화 세대와 명시적 Invalidate 또는 변경 추적을 제공하고, hit의 파괴 여부를 검사한다. negative cache는 새 객체 추가를 적절하게 무효화할 수 있을 때 도입한다. 단순 target 유효성 검사만으로 동일 이름의 우선 검색 대상 변경까지 해결되는 것은 아니다.

### 14. SerializeReferenceListDrawer의 타입 추론과 캐시 수명

GetFieldInfoFromProperty는 루트 targetObject 타입에서 `property.name`만 검색한다. `settings.actions`, `entries.Array.data[0].actions`, 상속된 private 필드처럼 경로 해석이 필요한 경우 실제 필드를 찾지 못한다. fallback이 typeof(object)이므로 파생 타입 목록이 의도치 않게 넓어지고 reflection·정렬·메뉴 생성 비용과 잘못된 후보가 함께 증가할 수 있다.

목록 후보는 abstract/generic만 걸러 직렬화 가능한지, 기본 생성자를 사용할 수 있는지 확인하지 않는다. Activator.CreateInstance 실패 전에 arraySize를 늘리므로 실패 복구 정책도 필요하다. concrete base type 자체를 추가하고 싶다면 현재 DerivedFrom 목록과 별도로 처리해야 한다.

캐시는 propertyPath만 키로 사용하고 callbacks는 처음 얻은 listProperty, label, derivedTypes를 캡처한다. drawer가 여러 소유 객체에 재사용되거나 배열 항목·SerializedObject가 교체될 때 같은 경로가 다른 데이터를 가리킬 수 있다. 해당 재사용의 실제 발생 여부는 Unity Inspector 검증이 필요하며 현재 장면에서 관찰한 버그라고 단정하지 않는다. dictionary에는 명시적 무효화도 없다.

그리기마다 요소용 SerializedProperty를 가져오고 Type.Name으로 GUIContent를 새로 만든다. 초기 LINQ/ToArray 비용은 캐시 miss 때의 비용이므로 매 Repaint 비용과 구분한다.

개선 방향: fieldInfo 활용 여부와 중첩·상속 경로 해석을 정리한다. 캐시는 소유 SerializedObject/대상과 propertyPath를 구분하고, 최신 property를 재조회하거나 유효성을 확인한다. 타입별 GUIContent는 캐시하고 생성 불가능한 후보를 제외한다. 첫 GetPropertyHeight가 고정 2줄을 반환하는 점은 최초 표시 레이아웃 검증 대상이다.

### 15–17. 예외 처리와 에디터 수명 정리

ContinueWith의 try에는 원래 task await와 성공 콜백 실행이 함께 들어 있다. 성공 콜백이 예외를 던지면 실패 콜백을 호출한다. Anyway는 성공·실패 콜백에 같은 onEnd를 연결하므로 정상 작업 후 onEnd가 실패할 때 같은 종료 처리가 다시 실행될 수 있다. 콜백이 일부 자원을 해제한 뒤 예외를 던지는 상황에서 이중 정리가 발생할 수 있다. 작업 완료 상태와 콜백 실패 정책을 분리하거나 finally에서 종료 처리를 정확히 한 번 실행하는 계약이 필요하다.

Horizontal/Vertical/DrawGUIElement는 GUI.enabled와 GUI.color를 true/white로 되돌린다. 외부의 disabled/tint 상태를 잃고 중첩 필드가 다시 활성화될 수 있다. contents가 예외를 던지면 EndHorizontal/EndVertical 및 상태 복구에 도달하지 않는다. 이전 상태 저장과 finally 또는 Unity scope를 사용해야 한다. 단순 성능 문제보다 사용자 입력과 GUI 상태의 정확성 문제다.

EditorCrowRx의 OnDisable은 OnExit 후 이벤트를 해제하고, EditorWindowCrowRx도 Exit 후 해제한다. 파생 종료 콜백이 예외를 던지면 static SceneView/EditorApplication 이벤트가 인스턴스를 계속 참조할 수 있다. finally에서 해제와 필드 정리를 보장해야 한다. 현재 파생 클래스에서 그런 예외를 관찰한 것은 아니며, 베이스 클래스의 방어 범위 문제다.

EditorWindowCrowRx의 Instance와 s_IsEnter는 닫힌 제네릭 타입별 static이다. 같은 타입의 창을 여러 개 만들 경우 마지막 창이 Instance를 덮어쓰고 다른 창 OnDisable도 Instance를 null로 만든다. OnEnable은 s_IsEnter=false 직후 true로 바꾸므로 static 진입 가드는 중복 진입 방지 역할을 하지 못한다. 단일 창 제한이 의도된 계약인지 먼저 명시해야 한다. SceneGUI 구독 조건은 인터페이스 구현 여부지만 실제 호출은 OnSceneGUI override이므로 인터페이스 메서드만 구현해도 호출된다고 생각하면 안 된다.

### 18–20. 수학 유틸리티의 오답과 불필요한 비용

`Mathm.Lerp(this Matrix4x4 org, ...)`는 org를 값으로 받는다. 내부 ref 수정은 지역 복사본에만 반영되고 반환도 void이므로 호출자 행렬이 변하지 않는다. ref receiver 또는 결과 반환 계약이 필요하다.

GetIntersectPosition의 공선 분기는 선분 구간이 겹치는지 확인하지 않는다. XY에서 source `(0,0)→(1,0)`, target `(2,0)→(3,0)`이면 떨어져 있지만 true와 `(2,0)`을 반환하는 흐름이다. 시작점이 같은 비평행 선분은 originalT/originalS가 동시에 0이라는 이유로 false가 될 수 있다. 끝점 포함과 공선 겹침 계약을 정리해야 한다.

Collider.Contains의 비Rigidbody 분기는 이미 world 좌표인 bounds.center를 다시 rotation 및 position과 합친다. 회전 없는 BoxCollider가 world x=10에 있고 x 범위가 9~11이라고 하면, worldPoint x=12의 closest는 x=11인데 계산 원점은 x=20이 된다. 두 접촉 방향이 모두 음의 x이므로 각도 0으로 바깥 점을 true로 판정한다. 포함 판정에는 ClosestPoint와 worldPoint의 일치/허용오차를 사용할 수 있지만 비활성 collider와 지원 collider 종류 등의 의미를 먼저 정해야 한다.

SpringDampen의 strength=0이면 dampeningFactor=1, 분자는 0, Log(1)=0이므로 0/0으로 NaN을 만든다. strength>1000이면 음수의 Log가 되어 NaN 경로가 있다. 0 강도의 극한값과 지원 강도 범위를 명시하고 처리해야 한다.

LerpAsync는 token을 검사하기 전에 setter(endValue) 또는 setter(startValue)를 실행한다. 사전 취소된 token도 값을 바꾸고, while 조건으로 빠져나가면 성공으로 끝날 수 있다. 루프 내 Yield 취소는 예외를 던지므로 취소 결과가 위치에 따라 달라진다. setter가 token을 취소하는 경우에도 동일한 구분이 생길 수 있다. 시작 시 ThrowIfCancellationRequested 및 일관된 종료 정책이 필요하다.

성능·GC의 별도 개선 후보는 다음과 같다.

- `Mathm.Intersect(Rect, ...)`는 조기 반환을 통과한 호출마다 Vector2[4]를 생성한다(`:73`). 네 개 지역 변수로 대체할 수 있는 직접적인 할당이다.
- `RectIntersect`는 양 끝점의 포함 검사를 각각 순회하고 bounds를 Min/Max 4회 순회한다(`:192–205`). 현재 ZLinq 경로를 일반 LINQ GC 문제라고 부를 근거는 없지만, bounds는 한 번의 루프 또는 재사용 가능한 사전 계산으로 만들 수 있다. 함수는 실제 edge 검사에서 rect[0..3]만 사용하므로 4점 입력 계약을 검사·문서화해야 한다.
- `IsArrival`은 Dot 부호만 필요하면서 방향을 normalized로 만든다(`:33`). 0 벡터/동일 지점에서 원하는 의미를 확인하면 정규화를 생략할 수 있다. GC보다 산술 비용 개선이며 측정 우선순위는 낮다.
- 곡선 Lerp는 마지막 rate가 1을 넘을 수 있고 Evaluate 결과로 보간한다. 원하는 끝점 도달을 보장할지 curve가 끝점을 정의하게 할지 계약 확인이 필요하다. Time.time/realtimeSinceStartup 기반 경과 계산과 UniTask async 사용 자체를 매 프레임 managed 할당이라고 단정하지 않는다.

## 추가 성능·GC 관찰

| 위치 | 확인된 구조 / 판단 | 개선 우선순위 |
|---|---|---|
| `Runtime/MonoBehaviourCrowRx.cs:29–35,57–63,83–88` | 최초 캐시 접근 때 R3 트리거 컴포넌트·Subject·구독 경로를 사용한다. 설치된 트리거 확장은 GetOrAddComponent이므로 같은 GameObject에서는 공유된다. 반복 getter가 매번 새 구독을 만드는 것은 아니다. 대량 생성 객체에서는 초기 비용 측정 필요. | P3 |
| 같은 파일의 rectTransform getter | RectTransform이 없으면 부재를 캐시하지 않아 매 접근 TryGetComponent가 반복된다. 파괴/제거 콜백은 disposable 필드만 null로 만들고 실제 객체 필드를 비우지 않는다. 이를 자동적인 영구 누수라고 부를 수는 없지만 캐시 수명 계약과 README의 'cleared' 설명을 맞출 필요가 있다. | P3 |
| `Runtime/Tasks/WhenAny.cs:13` | 호출마다 WhenAnyPromise를 new한다. 설치된 UniTask WhenAny도 같은 방식이므로 독자적인 퇴행이라고 부르지 않는다. 부분 배열 API의 호출 빈도가 높다면 할당 후보. | P3 |
| `Runtime/Tasks/Internal/StateTuple.cs:30–54` | warm-up 이후 tuple 재사용, 반환 시 참조 3개를 default로 지우는 점은 적절하다. ConcurrentQueue에 상한/정리 정책이 없어 최고 동시 사용량 수준의 tuple 보유가 남을 수 있다. 영구 누수나 항목 참조 누수는 확인되지 않았다. | P3 |
| WhenAny의 미완료 참가 작업 | 결과가 먼저 나와도 나머지 작업은 취소되지 않고 완료될 때까지 tuple/awaiter가 promise를 참조한다. 일반적인 WhenAny 계약이지만 반복 race에서 무한 대기 작업을 넣으면 수명이 길어진다. promise를 승자 완료 직후 풀로 반환하는 최적화는 늦은 callback 때문에 위험하다. | 계약 확인 |
| `Runtime/StringExtension.cs:44–58` | 현재 일반 StringBuilder, 부분 문자열 self[1..], 최종 문자열이 만들어진다. ZString 경로도 최종 문자열과 현재 부분 문자열까지 자동으로 없애지는 않는다. span append/첫 글자 변경 필요 여부로 중간 비용을 줄일 수 있다. | P3 |
| `Runtime/GameObjectExtension.cs:177–224` | 계층 경로를 부모마다 앞에 붙여 새 문자열을 만든다. 전체 복사량은 깊이에 대해 대략 제곱 증가. 호출 빈도가 높으면 노드 수집 후 1회 조립 또는 계층 변경 시 캐시 필요. 결과 문자열 할당 자체는 API 특성상 남는다. | P3 |
| `Runtime/TypeExtension.cs:25–86` | 제네릭 이름마다 StringBuilder·부분 문자열·generic argument 배열과 재귀 문자열 생성. 캐시 초기화에서만 쓴다면 P3. 중첩 제네릭은 첫 backtick에서 잘라 선언 타입 이름을 잃어 서로 다른 타입이 같은 이름을 얻을 수 있다. 캐시 식별자는 AssemblyQualifiedName 등과 표시명을 구분해야 한다. | P2 정확성 / P3 GC |
| `Runtime/TypeExtension.cs:8–20` | isIncludeSelf=false도 동일 타입에 대한 IsAssignableFrom이 true이므로 자기 자신을 제외하지 않는다. | P2 정확성 |
| `Runtime/ComponentExtension.cs:27`, `Runtime/GameObjectExtension.cs:72` | count=0도 예외 객체/문자열을 만들어 로그하는 Try API다. 반복적인 정상 부재 검사로 사용하면 에디터/로그 활성 빌드에 비용이 커진다. 메시지의 'at most one'과 count==1 판정도 다르다. UnityLog Conditional이 제거되는 빌드에서는 해당 호출과 인수 평가도 제거된다. | 계약 확인 후 P3 |
| `Editor/ComponentExtension.cs:31–53` | 매 호출 reflection 검색과 값 타입 결과 boxing. VariableType.Field.Equals(variableType) 등의 enum object 인수도 boxing 후보다. 메타데이터 캐시와 == 비교가 단순 개선 방향. | P3 |
| `Editor/SerializeReferenceListDrawer.cs:48` | 요소를 그릴 때마다 GUIContent 생성. 타입별 캐시 가능. 메뉴 열 때의 메뉴·콜백 생성은 사용자 이벤트 비용이다. | P3 |
| `Editor/DebugUtility.cs:307–339` | sphere 선 그리기는 약 4s²−2s개의 DrawLine 호출과 삼각함수 연산. GC보다 CPU/렌더 호출 증가 문제. 호출 수·segments를 제한하거나 형상을 캐시한 뒤 측정. | P3 |
| `Editor/GizmosUtility.cs:10–31,234–251` | 정적 형상 배열은 초기 1회 생성 후 재사용한다. Vector3/Vector4 지역 값 생성은 managed 객체 new로 취급하면 안 된다. | 현재 구조 적절 |

DebugUtility에는 별도의 시각적 정확성 문제가 있다. DrawBox의 연결선은 position을 더하지 않는다(`:356–359`). DrawArcXZ/DrawArcYZ의 회전은 각각 코드의 축 이름과 일치하는지 확인해야 한다(`:164,172`); XY 원을 Y축 -90도로 돌리면 YZ 평면이 되고, X축 90도로 돌리면 XZ 평면이 된다. GizmosUtility.DrawPlane은 normal이 Vector3.down일 때 보조 up과 평행해 cross가 0이 되고, 임의 normal에서도 축 정규화를 하지 않아 크기가 달라진다(`:175–183`). DrawCollider의 Capsule 분기는 direction을 무시하고 Y축을 사용한다(`:272`). 성능 판단과 별개로 실제 디버그 도형의 신뢰도에 영향을 준다.

## 수정 순서와 후속 측정 제안

1. **수명·결과 오류**: CTS 필드 정리, 제네릭 await, 콜백 정확히 1회, 행렬/교차/포함 판정, 취소 계약부터 해결한다. 잘못된 결과를 빠르게 계산하는 최적화는 의미가 없다.
2. **에디터의 반복 비용**: 무조건 Repaint를 줄인 뒤 GUI 람다와 PropertyEditViewer reflection 비용을 검토한다. GUI 헬퍼 최적화 전후는 같은 이벤트 수로 비교해야 한다.
3. **런타임의 불필요한 할당·탐색**: GatherComponent 배열, Rect 교차의 고정 배열, 이벤트 전달의 임시 객체, 중복 상하 검색, ArrayPool 참조 정리를 검토한다. 기존 append/includeInactive/순서 계약을 유지하거나 변경을 명시한다.
4. **빈도가 확인된 경로만 추가 최적화**: 타입 검색 cold start, 문자열 조립, R3 초기화, WhenAny promise, CTS 전환 비용을 실제 소비 프로젝트에서 측정한다.

다음 검증은 제안이며 이번 검토에서 실행하지 않았다.

| 대상 | 비교 조건 | 확인할 지표 / 동작 |
|---|---|---|
| Inspector/EditorWindow | 같은 필드 수, 입력 없는 상태, Repaint 정책 변경 전후 | Editor CPU, GUI 호출 수, GC.Alloc 호출 스택 |
| 계층 수집·상하 검색 | 얕고 넓은 계층 / 깊은 체인, 발견 / 부재, inactive 포함 | 시간·할당, 결과 순서와 append 여부 |
| 페이드 | 완료 후 재호출, 완료 전 재호출, OnDisable, 0 duration, 사전 취소 | 예외, 정리 횟수, 시작 비용, 잔존 CTS/작업 |
| continuation | 콜백 null, task 실패/취소, 콜백 자체 실패 | await 소비 여부, 종료 콜백 횟수, 예외 전달 |
| 이벤트 전달 | mouse/touch, handler 없음, 여러 raycast 결과, 중첩 호출 | 반환값, 좌표, 이벤트 배송, 호출당 할당 |
| 풀·race | 대량 사용 후 idle, 완료하지 않는 loser 포함 | managed 보유 참조와 작업 수, peak/idle 메모리 |
| 런타임 성능 | Editor와 실제 대상 Player를 분리, cold/warm 분리 | GC.Alloc, 프레임 시간, 수집 횟수; 메모리 보유는 별도 snapshot |

이 프로젝트는 유틸리티 패키지에 가깝고 실제 소비 호출이 적다. 반복 횟수를 임의로 넣은 synthetic 측정만으로 QV 등 소비 프로젝트의 체감 성능을 결론 내리지 말아야 한다.

## 최초 파일별 검토 기록 (수정 전 이력)

| 파일 | 결과 연결 |
|---|---|
| Runtime/CancellationTokenSourceExtension.cs | Cancel 콜백 예외 시 Dispose에 도달하지 않음. finally와 소유권 정책 필요. disposed CTS 재사용 방지는 호출자 책임과 함께 검토. |
| Runtime/ComponentExtension.cs | 풀 리스트 사용은 적절. Try 부재 로그·계약, 반복 native 접근은 추가 관찰 항목. |
| Runtime/GameObjectExtension.cs | 10, hierarchy 문자열 조립. 간단한 GetOrAddComponent의 TryGetComponent 사용은 적절. |
| Runtime/Mathm.cs | 18–20, Rect 배열·bounds 반복 순회. |
| Runtime/MonoBehaviourCrowRx.cs | 캐스팅 기반 rectTransform, 명시적 Transform 갱신, 단일 파괴 정리 및 Awake 이전 감시로 수정. 최초 초기화 비용은 남음. |
| Runtime/RectTransformExtension.cs | 단순 값/Transform 변경. 직접 반복 할당 없음. 빈번 호출 시 layout/native setter 비용은 소비자가 결정. |
| Runtime/RuntimeTypeCache.cs | 11. |
| Runtime/SerializeReferenceListAttribute.cs | 12, wrapper 변환 초기 List. |
| Runtime/StringExtension.cs | 문자열 결과와 중간 StringBuilder/substring 비용. SubstringBetween의 결과 문자열은 API의 정상 비용. |
| Runtime/TransformExtension.cs | 06–08. |
| Runtime/TypeExtension.cs | 이름 생성, 중첩 제네릭 충돌, isIncludeSelf 계약 오류. |
| Runtime/UnityLog.cs | Conditional 호출 제거를 고려해야 함. 실제 logging 비용은 활성 심볼/로거/스택 추적 설정에 따라 다름. |
| Runtime/Helper/CanvasGroupFade.cs | 01, 시작 시 CTS/콜백, 완료·취소 수명. |
| Runtime/Helper/FindObjectHelper.cs | 13. |
| Runtime/Helper/TriggerEventListener.cs | 얇은 UnityEvent 전달. 자체 매 프레임 코드 없음. listener 비용과 UnityEvent 초기 비용은 별도 범위. |
| Runtime/Tasks/ContinueWithAnyway.cs | 02, 15. |
| Runtime/Tasks/ForgetSafe.cs | try/await/catch 래퍼. async 자체를 무조건 할당으로 판정하지 않음; 취소 로그 활성 시 문자열 비용. |
| Runtime/Tasks/RunSafe.cs | 09. |
| Runtime/Tasks/WhenAny.cs | promise 할당, loser 수명. 현재 인수 검증 개선은 확인됨. |
| Runtime/Tasks/Internal/StateTuple.cs | 참조 정리 및 재사용은 적절. 무제한 peak 보유는 측정 후보. |
| Editor/EditorCrowRx.cs | 03, 17. 현재 미커밋 버전의 인스턴스별 구독/해제를 기준으로 검토. |
| Editor/EditorCrowRxExtension.cs | 04, 16. UITK의 AddDefaultPropertiesExcluding은 생성 시 필드/Copy/VisualElement 비용이며 자체 매 프레임 반복 경로 없음. |
| Editor/EditorWindowCrowRx.cs | 03–05, 17, static 창 상태와 타입 popup 계약. |
| Editor/ComponentInspectorCrowRx.cs | 타깃 accessor와 reflection GetValue로 전달하는 얇은 래퍼. |
| Editor/ComponentExtension.cs | reflection 재검색·boxing 후보. |
| Editor/DebugUtility.cs | sphere O(s²) 선 호출, Box 연결선과 arc 평면. |
| Editor/GizmosUtility.cs | 정적 배열 재사용은 적절. 평면 basis 및 capsule 축 정확성. |
| Editor/EditorWindowCrowRxSerializeFieldAttribute.cs | 단순 속성 정의. 검색/인스턴스 생성 비용은 PropertyEditViewer 쪽. |
| Editor/IEditorCrowRx.cs | marker interface. 자체 실행/할당 경로 없음. |
| Editor/IOnDuringSceneGui.cs | callback 계약. 자체 실행/할당 경로 없음. |
| Editor/IOnHierarchyChanged.cs | callback 계약. 자체 실행/할당 경로 없음. |
| Editor/PrefabInformation.cs | 데이터 holder와 PrefabUtility 조회. 생성자만으로 public 상태 필드를 채우지 않으므로 소비자의 초기화 책임 문서화 필요. 직접 반복 할당 없음. |
| Editor/RuntimeTypeCacheEditor.cs | 수동 GatherTypes 메뉴. 비용과 실패 동작은 11. |
| Editor/ScriptableObjectInspectorCrowRx.cs | 타깃 캐시. 자체 매 GUI 할당 없음. |
| Editor/ScriptReloadOrder.cs | enum. 실행/할당 경로 없음. |
| Editor/SerializeReferenceListDrawer.cs | 14, 요소 GUIContent. |

EditorWindowCrowRx의 타입 Popup(`:311–329`)은 value.Name으로 항목을 찾은 뒤 Type.GetType(content[result])로 역변환한다. 단순 타입 이름은 일반적으로 충분한 식별자가 아니며, value null 또는 content 빈 배열에서도 실패 경로가 있다. 표시 문자열과 실제 Type[]를 분리하는 편이 안전하다. 같은 위치의 string Popup도 음수 선택 인덱스를 검사하지 않으므로 빈 항목·선택 없음 조건을 GUI에서 확인해야 한다.

분석용 외부 컨텍스트 저장 도구는 자동 승인 검토에서 소스 공개 위험을 이유로 거부되었다. 해당 서비스에 코드를 보내지 않고 로컬 읽기로 검토를 완료했다. 외부 조회에는 일반적인 공식 API 문서 검색만 사용했다.
