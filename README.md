**개요:**
windows tray icon application.  
http 및 websocket 수신 (http://localhost:8080, http://localhost:8080/ws)  
http://localhost:8080/?data=abcde 로 호출받으면, 연결된 websocket 클라이언트에게 {"data":"abcde"}로 데이터 전송  
  
  
**폴더 설명:**  
TrayIconAppTest: C# 코드 (테스트 서버)  
nodeClient: node.js websocket 테스트 클라이언트  

**어플리케이션 컴파일:**  
Visual Studio Community 2022  
Nuget package: Newtonsoft.Json  
Target .net version: 4.7.2  
(Release.zip에 실행 파일 들어있음)  

**node.js 어플리케이션 실행**  
Node.js, npm 설치되어 있어야 함.  
해당 폴더에서 npm install  
node test.js 로 실행  
