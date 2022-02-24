# DingtalkDemo
钉钉机器人接口Demo

1、配置 appsettings.json
```
"DingtalkRobot": {
    "Webhook": "",  //填自己机器人 Webhook 地址
    "Secret": "",   //加密模式需要的secret
    "AppSecret": "" //钉钉开发者后台 应用凭证 AppSecret
}
```
2、接收机器人信息地址： 根域名+/robots 
>列如：http://www.myhost.com/robots