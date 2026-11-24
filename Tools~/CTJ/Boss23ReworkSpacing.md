# 기능별 빈 줄 정리 검증

작업일: 2026-11-24

검토 10개, 변경 8개, 빈 줄 추가 96개.

모든 변경 파일에서 C# 토큰과 비어 있지 않은 원문 줄이 전후 동일하다. 아래 해시는 공백·주석을 제외한 코드 토큰을 기준으로 하며 전후 같은 값이다. 기존 들여쓰기, 줄 내용, 문자열, 주석과 파일 BOM·줄바꿈 형식은 유지했다.

이 표는 빈 줄 정리 단계의 기록이다. 이후 실행 검증에서 EnemyAfterImageModule의 풀 정리 콜백에 파괴된 Unity 오브젝트의 null 확인을 추가했다. 해당 기능 수정은 서식 변경과 별도이며, 아래 해시는 최종 파일 전체의 검증값을 뜻하지 않는다.

| 파일 | 추가 빈 줄 | 전후 동일 토큰 SHA256 |
| --- | ---: | --- |
| Assets/_Scripts/Agent/Enemies/Bosses/ProductionControlDrone.cs | 42 | 8039140E67D15CC42610DD45136ED1BF86F728CE91B24BDA7B87AB83707C1343 |
| Assets/_Scripts/Agent/Enemies/EnemyAfterImageModule.cs | 1 | BEFB5E5C7B4EAA6706C971E4F99169F134A61216F2862BDF5155A3BB9D2B3FCF |
| Assets/_Scripts/Agent/Enemies/Skills/DroneChargeSkill.cs | 8 | B19E2FB6DA2D840045130A69ED1037E9B59181AC72A7D4954C6FC6BABB0A1E38 |
| Assets/_Scripts/Agent/Enemies/Skills/DroneAirTransitionSkill.cs | 3 | 0B4D550419366C2824C0BC0B984F42244E39A841001C72FD8A9D2AD213350AF7 |
| Assets/_Scripts/Agent/Enemies/Skills/DroneSalvoSkill.cs | 7 | 7395DC0933066710EE5BA750B2A1F27BE8D048537C626A85976DA9F596C9F48F |
| Assets/_Scripts/Agent/Enemies/Skills/DroneDownwardLaserSkill.cs | 8 | 8BAE3FADB3E72CC29D7832887F4288598BD77F32BCA4193E447A1E683F56EB28 |
| Assets/_Scripts/Agent/Enemies/Skills/DroneBombSkill.cs | 15 | 720C9B8A2B497F10A5FBAE12F1155421F49722243C95B99B0D71B97B47196B28 |
| Assets/_Scripts/UI/Enemy/DroneBombTargetGraphic.cs | 12 | 2F12544730EB699546F146C4EC7050053167DC7B358F9AD54A98A89205315187 |
