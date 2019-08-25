## web.config

Optionally this plugin can place a `web.config` next to the validation file, to 
help IIS properly serve the response. 
There are [many reasons](/win-acme/manual/validation-problems) why IIS can fail to 
properly server the file and some of them can be solved this way. 

### Changing the template

The web.config that will be copied lives in the root of the program directory with the 
name `web_config.xml`. You can modify it to fit your needs, e.g. for MVC sites you might need:

```XML
<configuration>
    <system.webServer>
        <staticContent>
            <clear/>
            <mimeMap fileExtension = ".*" mimeType="text/json" />
        </staticContent>
        <handlers>
            <clear />
            <add name="StaticFile" path="*" verb="*" type="" modules="StaticFileModule,DefaultDocumentModule,DirectoryListingModule" scriptProcessor="" resourceType="Either" requireAccess="Read" allowPathInfo="false" preCondition="" responseBufferLimit="4194304" />
        </handlers>
    </system.webServer>
</configuration>
```

Or to disable URL Rewriting you might need to add this (in the beginning, right after `<clear />`).

```XML
<rule name="LetsEncrypt Rule" stopProcessing="true">
    <match url="^\.well-known.*$" />
    <action type="None" />
</rule>
```