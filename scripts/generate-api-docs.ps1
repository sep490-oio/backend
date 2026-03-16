[CmdletBinding()]
param(
    [switch]$BootstrapDescriptions,
    [string]$RepoRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $scriptDirectory = Split-Path -Parent $PSCommandPath
    $RepoRoot = Split-Path -Parent $scriptDirectory
}

function Ensure-Directory {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Get-Indent {
    param([string]$Text)

    if ($Text -match '^\s+') {
        return $matches[0].Length
    }

    return 0
}

function Split-TopLevel {
    param(
        [string]$InputText,
        [char]$Separator = ','
    )

    if ([string]::IsNullOrWhiteSpace($InputText)) {
        return @()
    }

    $parts = New-Object System.Collections.Generic.List[string]
    $current = New-Object System.Text.StringBuilder
    $angleDepth = 0
    $parenDepth = 0
    $bracketDepth = 0
    $braceDepth = 0
    $inString = $false

    for ($index = 0; $index -lt $InputText.Length; $index++) {
        $char = $InputText[$index]

        if ($char -eq '"') {
            $inString = -not $inString
            [void]$current.Append($char)
            continue
        }

        if (-not $inString) {
            switch ($char) {
                '<' { $angleDepth++ }
                '>' { if ($angleDepth -gt 0) { $angleDepth-- } }
                '(' { $parenDepth++ }
                ')' { if ($parenDepth -gt 0) { $parenDepth-- } }
                '[' { $bracketDepth++ }
                ']' { if ($bracketDepth -gt 0) { $bracketDepth-- } }
                '{' { $braceDepth++ }
                '}' { if ($braceDepth -gt 0) { $braceDepth-- } }
            }

            if ($char -eq $Separator -and $angleDepth -eq 0 -and $parenDepth -eq 0 -and $bracketDepth -eq 0 -and $braceDepth -eq 0) {
                $parts.Add($current.ToString().Trim())
                $current.Clear() | Out-Null
                continue
            }
        }

        [void]$current.Append($char)
    }

    $last = $current.ToString().Trim()
    if ($last.Length -gt 0) {
        $parts.Add($last)
    }

    return $parts.ToArray()
}

function Split-PascalCase {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return ([regex]::Replace($Value, '([a-z0-9])([A-Z])', '$1 $2'))
}

function Escape-Yaml {
    param([string]$Value)

    $text = $Value
    if ($null -eq $text) {
        $text = ""
    }

    $text = $text.Replace('\', '\\').Replace('"', '\"')
    return '"' + $text + '"'
}

function Unescape-Yaml {
    param([string]$Value)

    $text = $Value.Trim()
    if ($text.StartsWith('"') -and $text.EndsWith('"')) {
        $text = $text.Substring(1, $text.Length - 2)
    }

    return $text.Replace('\"', '"').Replace('\\', '\')
}

function ConvertTo-AnchorId {
    param([string]$Value)

    $normalized = $Value.ToLowerInvariant()
    $normalized = [regex]::Replace($normalized, '[^a-z0-9]+', '-')
    $normalized = $normalized.Trim('-')

    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return "schema-unknown"
    }

    return "schema-$normalized"
}

function Get-PrimitiveTypeName {
    param([string]$TypeName)

    $clean = $TypeName.Trim()
    $clean = $clean.TrimEnd('?')

    $primitiveMap = @{
        "string" = "string"
        "bool" = "boolean"
        "boolean" = "boolean"
        "byte" = "number"
        "sbyte" = "number"
        "short" = "number"
        "ushort" = "number"
        "int" = "number"
        "uint" = "number"
        "long" = "number"
        "ulong" = "number"
        "float" = "number"
        "double" = "number"
        "decimal" = "number"
        "Guid" = "guid"
        "DateTime" = "datetime"
        "DateTimeOffset" = "datetime-offset"
        "DateOnly" = "date"
        "TimeSpan" = "duration"
        "IPAddress" = "ip-address"
        "IFormFile" = "file"
        "Stream" = "stream"
        "object" = "object"
    }

    if ($primitiveMap.ContainsKey($clean)) {
        return $primitiveMap[$clean]
    }

    return ""
}

function Get-CollectionElementType {
    param([string]$TypeName)

    $trimmed = $TypeName.Trim().TrimEnd('?')
    if ($trimmed.EndsWith("[]")) {
        return $trimmed.Substring(0, $trimmed.Length - 2)
    }

    if ($trimmed -match '^(?:IReadOnlyCollection|IReadOnlyList|IEnumerable|ICollection|IList|List|HashSet)<(.+)>$') {
        return $matches[1].Trim()
    }

    return ""
}

function Is-GenericPlaceholder {
    param([string]$TypeName)

    $trimmed = $TypeName.Trim().TrimEnd('?')
    return $trimmed -match '^T(?:[A-Z]\w*)?$'
}

function Parse-ParameterToken {
    param([string]$Token)

    $clean = $Token.Trim()
    $required = $clean -match '\[Required\]'
    $clean = [regex]::Replace($clean, '\[[^\]]+\]\s*', '')

    $defaultValue = $null
    if ($clean -match '=(.+)$') {
        $defaultValue = $matches[1].Trim()
        $clean = $clean.Substring(0, $clean.LastIndexOf('=')).Trim()
    }

    if ($clean -notmatch '^(?<type>.+?)\s+(?<name>\w+)$') {
        return $null
    }

    return [pscustomobject]@{
        Name = $matches['name']
        Type = $matches['type'].Trim()
        Required = $required
        DefaultValue = $defaultValue
        Source = "parameter"
    }
}

function Get-MatchingBraceIndex {
    param(
        [string]$Content,
        [int]$OpenBraceIndex
    )

    $depth = 0
    for ($index = $OpenBraceIndex; $index -lt $Content.Length; $index++) {
        $char = $Content[$index]
        if ($char -eq '{') {
            $depth++
        }
        elseif ($char -eq '}') {
            $depth--
            if ($depth -eq 0) {
                return $index
            }
        }
    }

    throw "Cannot find matching brace in source text."
}

function Parse-PropertyMembers {
    param([string]$Body)

    $properties = New-Object System.Collections.Generic.List[object]
    $pattern = '(?ms)(?<attributes>(?:\s*\[[^\]]+\]\s*)*)public\s+(?<type>[\w<>\[\],\.\?\s]+?)\s+(?<name>\w+)\s*\{\s*get;\s*(?:init;|set;|private set;|protected set;|internal set;)?'
    foreach ($match in [regex]::Matches($Body, $pattern)) {
        $typeName = $match.Groups['type'].Value.Trim()
        $name = $match.Groups['name'].Value.Trim()
        $attributes = $match.Groups['attributes'].Value
        $required = $attributes -match '\[Required\]'

        $properties.Add([pscustomobject]@{
            Name = $name
            Type = $typeName
            Required = $required
            DefaultValue = $null
            Source = "property"
        })
    }

    return $properties.ToArray()
}

function Parse-CsTypesFromFile {
    param(
        [string]$FilePath,
        [switch]$IncludeNested,
        [string]$EndpointClassName
    )

    $content = Get-Content -LiteralPath $FilePath -Raw
    $namespace = ""
    if ($content -match '(?m)^namespace\s+([A-Za-z0-9_\.]+)\s*;') {
        $namespace = $matches[1]
    }

    $pattern = '(?ms)public\s+(?:sealed\s+|abstract\s+|partial\s+|static\s+)*(?<kind>record|class|interface|enum)(?:\s+class|\s+struct)?\s+(?<name>\w+)(?<generic><[^>{;\r\n]+>)?\s*(?:\((?<params>.*?)\))?\s*(?::\s*(?<bases>[^{;]+))?\s*(?<ender>\{|;)'
    $types = New-Object System.Collections.Generic.List[object]

    foreach ($match in [regex]::Matches($content, $pattern)) {
        $name = $match.Groups['name'].Value
        if (-not $IncludeNested) {
            $lineStart = $content.LastIndexOf("`n", $match.Index)
            if ($lineStart -lt 0) { $lineStart = 0 } else { $lineStart++ }
            $line = $content.Substring($lineStart, [Math]::Min($content.Length - $lineStart, $match.Length + 32))
            if ((Get-Indent $line) -gt 0) {
                continue
            }
        }
        $kind = $match.Groups['kind'].Value
        $bases = $match.Groups['bases'].Value.Trim()
        $paramText = $match.Groups['params'].Value
        $ender = $match.Groups['ender'].Value
        $body = ""
        if ($ender -eq "{") {
            $openBraceIndex = $content.IndexOf("{", $match.Index + $match.Length - 1)
            $closeBraceIndex = Get-MatchingBraceIndex -Content $content -OpenBraceIndex $openBraceIndex
            $body = $content.Substring($openBraceIndex + 1, $closeBraceIndex - $openBraceIndex - 1)
        }

        $fields = New-Object System.Collections.Generic.List[object]
        foreach ($token in (Split-TopLevel -InputText $paramText)) {
            $field = Parse-ParameterToken -Token $token
            if ($null -ne $field) {
                $fields.Add($field)
            }
        }

        if ($fields.Count -eq 0 -and $body.Length -gt 0 -and $kind -ne "enum") {
            foreach ($property in (Parse-PropertyMembers -Body $body)) {
                $fields.Add($property)
            }
        }

        $fullName = if ($IncludeNested) {
            "$EndpointClassName.$name"
        }
        elseif ([string]::IsNullOrWhiteSpace($namespace)) {
            $name
        }
        else {
            "$namespace.$name"
        }

        $types.Add([pscustomobject]@{
            Name = $name
            FullName = $fullName
            Namespace = $namespace
            Kind = $kind
            GenericText = $match.Groups['generic'].Value
            Bases = if ([string]::IsNullOrWhiteSpace($bases)) { @() } else { Split-TopLevel -InputText $bases }
            Fields = $fields.ToArray()
            Body = $body
            FilePath = $FilePath
            IsNested = [bool]$IncludeNested
        })
    }

    return $types.ToArray()
}

function Build-TypeIndex {
    param([string[]]$SourceRoots)

    $index = @{}
    foreach ($root in $SourceRoots) {
        foreach ($file in (Get-ChildItem -Path $root -Recurse -Filter *.cs | Where-Object { -not $_.FullName.Contains("\bin\") -and -not $_.FullName.Contains("\obj\") })) {
            foreach ($typeInfo in (Parse-CsTypesFromFile -FilePath $file.FullName)) {
                if (-not $index.ContainsKey($typeInfo.Name)) {
                    $index[$typeInfo.Name] = New-Object System.Collections.Generic.List[object]
                }

                $index[$typeInfo.Name].Add($typeInfo)
            }
        }
    }

    return $index
}

function Resolve-TypeInfo {
    param(
        [string]$TypeExpression,
        [hashtable]$TypeIndex,
        [string[]]$PreferredNamespaces
    )

    $trimmed = $TypeExpression.Trim()
    $trimmed = $trimmed.TrimEnd('?')
    $trimmed = [regex]::Replace($trimmed, '^(?:required\s+)', '')

    $elementType = Get-CollectionElementType -TypeName $trimmed
    if ($elementType) {
        return Resolve-TypeInfo -TypeExpression $elementType -TypeIndex $TypeIndex -PreferredNamespaces $PreferredNamespaces
    }

    if ($trimmed -match '^(?<outer>\w+)<(?<inner>.+)>$') {
        $outer = $matches['outer']
        if ($TypeIndex.ContainsKey($outer)) {
            return $TypeIndex[$outer][0]
        }
        return $null
    }

    if (-not $TypeIndex.ContainsKey($trimmed)) {
        return $null
    }

    $candidates = $TypeIndex[$trimmed]
    foreach ($namespace in $PreferredNamespaces) {
        $match = $candidates | Where-Object { $_.Namespace -eq $namespace } | Select-Object -First 1
        if ($null -ne $match) {
            return $match
        }
    }

    return $candidates[0]
}

function Resolve-UrlConstants {
    param([string]$FilePath)

    $content = Get-Content -LiteralPath $FilePath
    $classStack = New-Object System.Collections.Generic.List[string]
    $scopeStack = New-Object System.Collections.Generic.List[string]
    $values = @{}

    foreach ($line in $content) {
        if ($line -match '^\s*public static(?:\s+partial)? class\s+(\w+)') {
            $classStack.Add($matches[1])
            $scopeStack.Add("class")
            continue
        }

        if ($line -match '^\s*private const string\s+Base\s*=\s*"([^"]+)";') {
            $path = ($classStack -join '.')
            $values["$path.Base"] = $matches[1]
            continue
        }

        if ($line -match '^\s*public const string\s+(\w+)\s*=\s*(.+);\s*$') {
            $constName = $matches[1]
            $expr = $matches[2].Trim()
            $classPath = $classStack -join '.'
            $key = "$classPath.$constName"

            if ($expr -match '^"([^"]*)"$') {
                $values[$key] = $matches[1]
                continue
            }

            if ($expr -match '^\$\{Base\}/') {
                $baseValue = $values["$classPath.Base"]
                $suffix = $expr.Substring(7).Trim('"')
                $values[$key] = "$baseValue/$suffix"
                continue
            }

            if ($expr -eq "Base") {
                $values[$key] = $values["$classPath.Base"]
                continue
            }

            if ($expr -match '^\$"(?<template>.*)"$') {
                $template = $matches['template']
                $resolved = [regex]::Replace(
                    $template,
                    '\{(?<name>\w+)\}',
                    {
                        param($match)
                        $referenceName = $match.Groups['name'].Value
                        $referenceKey = "$classPath.$referenceName"
                        if ($values.ContainsKey($referenceKey)) {
                            return $values[$referenceKey]
                        }

                        return $match.Value
                    }
                )

                $resolved = $resolved.Replace('{{', '{').Replace('}}', '}')
                $values[$key] = $resolved
                continue
            }
        }

        if ($line -match '^\s*}\s*$' -and $scopeStack.Count -gt 0) {
            $lastScope = $scopeStack[$scopeStack.Count - 1]
            $scopeStack.RemoveAt($scopeStack.Count - 1)
            if ($lastScope -eq "class" -and $classStack.Count -gt 0) {
                $classStack.RemoveAt($classStack.Count - 1)
            }
        }
    }

    return $values
}

function Get-StatusLabel {
    param([int]$StatusCode)

    $map = @{
        200 = "200 OK"
        201 = "201 Created"
        204 = "204 No Content"
        400 = "400 Bad Request"
        401 = "401 Unauthorized"
        403 = "403 Forbidden"
        404 = "404 Not Found"
        409 = "409 Conflict"
        422 = "422 Unprocessable Entity"
    }

    if ($map.ContainsKey($StatusCode)) {
        return $map[$StatusCode]
    }

    return "$StatusCode"
}

function Infer-EndpointPurpose {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Path
    )

    $baseName = $Name -replace 'Endpoint$', ''
    $baseName = Split-PascalCase -Value $baseName

    $verbMap = @{
        "GET" = "Lay"
        "POST" = "Thuc hien"
        "PUT" = "Cap nhat"
        "PATCH" = "Cap nhat"
        "DELETE" = "Xoa"
    }

    $verb = if ($verbMap.ContainsKey($Method)) { $verbMap[$Method] } else { "Thuc hien" }
    return "$verb $baseName qua $Method $Path."
}

function Infer-Audience {
    param(
        [string]$Path,
        [string]$AuthRequirement,
        [string]$Permission
    )

    if ($AuthRequirement -eq "anonymous") {
        return "anonymous"
    }

    if ($Path.StartsWith("api/admin/") -or $Permission -match 'Admin') {
        return "admin"
    }

    if ($Path.StartsWith("api/me/")) {
        return "authenticated"
    }

    if ($Path.StartsWith("webhooks/")) {
        return "provider"
    }

    return "authenticated"
}

function Infer-HubPurpose {
    param(
        [string]$HubName,
        [string]$MemberName,
        [string]$Kind
    )

    $readable = Split-PascalCase -Value $MemberName
    if ($Kind -eq "event") {
        return "Server push $readable tren $HubName."
    }

    return "Goi realtime $readable tren $HubName."
}

function Load-Descriptions {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return @{
            http = @{}
            signalr_methods = @{}
            signalr_events = @{}
        }
    }

    $content = Get-Content -LiteralPath $Path
    $result = @{
        http = @{}
        signalr_methods = @{}
        signalr_events = @{}
    }

    $section = ""
    $key = ""
    foreach ($line in $content) {
        if ($line -match '^(http|signalr_methods|signalr_events):\s*$') {
            $section = $matches[1]
            continue
        }

        if ($line -match '^\s{2}"(.+)":\s*$') {
            $key = Unescape-Yaml -Value ('"' + $matches[1] + '"')
            if (-not $result[$section].ContainsKey($key)) {
                $result[$section][$key] = @{
                    purpose = ""
                    audience = ""
                    notes = ""
                }
            }
            continue
        }

        if ($line -match '^\s{4}(purpose|audience|notes):\s*(.+)\s*$') {
            $field = $matches[1]
            $value = Unescape-Yaml -Value $matches[2]
            $result[$section][$key][$field] = $value
        }
    }

    return $result
}

function Save-Descriptions {
    param(
        [string]$Path,
        [hashtable]$Descriptions
    )

    $builder = New-Object System.Text.StringBuilder
    foreach ($section in @("http", "signalr_methods", "signalr_events")) {
        [void]$builder.AppendLine("${section}:")
        foreach ($key in ($Descriptions[$section].Keys | Sort-Object)) {
            [void]$builder.AppendLine("  $(Escape-Yaml -Value $key):")
            $entry = $Descriptions[$section][$key]
            [void]$builder.AppendLine("    purpose: $(Escape-Yaml -Value $entry.purpose)")
            [void]$builder.AppendLine("    audience: $(Escape-Yaml -Value $entry.audience)")
            [void]$builder.AppendLine("    notes: $(Escape-Yaml -Value $entry.notes)")
        }
    }

    Set-Content -LiteralPath $Path -Value $builder.ToString() -Encoding UTF8
}

function Parse-EndpointFile {
    param(
        [string]$FilePath,
        [hashtable]$UrlMap,
        [hashtable]$TypeIndex
    )

    $content = Get-Content -LiteralPath $FilePath -Raw
    $usingNamespaces = [regex]::Matches($content, '(?m)^using\s+([A-Za-z0-9_\.]+);') | ForEach-Object { $_.Groups[1].Value }
    $endpointClassName = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)

    $mapMatch = [regex]::Match($content, '(?ms)app\.Map(?<verb>Get|Post|Put|Patch|Delete)\((?<url>ApiEndpoint\.Url\.[^,]+),\s*async\s*\((?<params>.*?)\)\s*=>')
    if (-not $mapMatch.Success) {
        return $null
    }

    $verb = $mapMatch.Groups['verb'].Value.ToUpperInvariant()
    $urlKey = $mapMatch.Groups['url'].Value.Trim()
    $urlPath = if ($UrlMap.ContainsKey($urlKey)) { $UrlMap[$urlKey] } else { $mapMatch.Groups['url'].Value }
    $lambdaParams = $mapMatch.Groups['params'].Value

    $nestedTypeInfos = Parse-CsTypesFromFile -FilePath $FilePath -IncludeNested -EndpointClassName $endpointClassName
    $nestedTypeMap = @{}
    foreach ($typeInfo in $nestedTypeInfos) {
        $nestedTypeMap[$typeInfo.Name] = $typeInfo
    }

    $authRequirement = "authenticated"
    if ($content -match '\.AllowAnonymous\(\)') {
        $authRequirement = "anonymous"
    }
    elseif ($content -match '\.RequireAuthorization\((?<permission>[^)]+)\)') {
        $authRequirement = "authorized"
    }

    $permission = ""
    if ($content -match '\.RequireAuthorization\((?<permission>[^)]+)\)') {
        $permission = $matches['permission'].Trim()
    }

    $tag = ""
    if ($content -match '\.WithTags\(ApiEndpoint\.Tags\.(\w+)\)') {
        $tag = $matches[1]
    }

    $hasIdempotency = $content -match 'IdempotencyFilter<'
    $excludeFromDescription = $content -match '\.ExcludeFromDescription\(\)'

    $requestType = $null
    if ($lambdaParams -match '(?<!\w)Request\s+request(?!\w)') {
        $requestType = $nestedTypeMap["Request"]
    }

    $queryType = $null
    if ($lambdaParams -match '\[AsParameters\]\s*Parameters\s+parameters') {
        $queryType = $nestedTypeMap["Parameters"]
    }

    $pathParams = New-Object System.Collections.Generic.List[object]
    foreach ($placeholder in [regex]::Matches($urlPath, '\{(?<name>\w+)(?::(?<constraint>[^}]+))?\}')) {
        $name = $placeholder.Groups['name'].Value
        $constraint = $placeholder.Groups['constraint'].Value
        $paramType = "string"
        if ($lambdaParams -match "(?<type>\w+(?:<[^>]+>)?)\s+$name\b") {
            $paramType = $matches['type']
        }
        elseif ($constraint) {
            $paramType = $constraint
        }

        $pathParams.Add([pscustomobject]@{
            Name = $name
            Type = $paramType
        })
    }

    $responses = New-Object System.Collections.Generic.List[object]
    foreach ($produce in [regex]::Matches($content, '\.Produces<(?<type>[^>]+)>\(StatusCodes\.Status(?<code>\d{3})')) {
        $responses.Add([pscustomobject]@{
            StatusCode = [int]$produce.Groups['code'].Value
            Type = $produce.Groups['type'].Value.Trim()
            Kind = "success"
        })
    }
    foreach ($produce in [regex]::Matches($content, '\.Produces\(StatusCodes\.Status(?<code>\d{3})')) {
        $responses.Add([pscustomobject]@{
            StatusCode = [int]$produce.Groups['code'].Value
            Type = $null
            Kind = "success"
        })
    }
    foreach ($produce in [regex]::Matches($content, '\.ProducesProblem\(StatusCodes\.Status(?<code>\d{3})')) {
        $responses.Add([pscustomobject]@{
            StatusCode = [int]$produce.Groups['code'].Value
            Type = "ProblemDetails"
            Kind = "error"
        })
    }
    if ($content -match '\.ProducesValidationProblem\(\)') {
        $responses.Add([pscustomobject]@{
            StatusCode = 400
            Type = "ValidationProblemDetails"
            Kind = "error"
        })
    }

    if (-not ($responses | Where-Object { $_.Kind -eq "success" })) {
        if ($content -match 'ToNoContentHttpResult\(\)') {
            $responses.Add([pscustomobject]@{ StatusCode = 204; Type = $null; Kind = "success" })
        }
        elseif ($content -match 'Results\.(Ok|Json)\(new\s*\{(?<object>.*?)\}\)' ) {
            $anonymousName = "$endpointClassName.SuccessPayload"
            $fields = New-Object System.Collections.Generic.List[object]
            foreach ($propertyToken in (Split-TopLevel -InputText $matches['object'])) {
                $cleanToken = $propertyToken.Trim().Trim(',')
                if ([string]::IsNullOrWhiteSpace($cleanToken)) {
                    continue
                }

                if ($cleanToken -match '^(?<name>\w+)\s*=\s*(?<value>.+)$') {
                    $fieldName = $matches['name']
                    $valueExpr = $matches['value'].Trim()
                    $fieldType = if ($valueExpr -match 'true|false|Is[A-Z]') { "bool" } elseif ($valueExpr -match '\d') { "number" } else { "string" }
                    $fields.Add([pscustomobject]@{
                        Name = $fieldName
                        Type = $fieldType
                        Required = $false
                        DefaultValue = $null
                        Source = "property"
                    })
                }
            }

            $nestedTypeMap["SuccessPayload"] = [pscustomobject]@{
                Name = "SuccessPayload"
                FullName = "$endpointClassName.SuccessPayload"
                Namespace = ""
                Kind = "record"
                GenericText = ""
                Bases = @()
                Fields = $fields.ToArray()
                Body = ""
                FilePath = $FilePath
                IsNested = $true
            }

            $responses.Add([pscustomobject]@{ StatusCode = 200; Type = "$endpointClassName.SuccessPayload"; Kind = "success" })
        }
        else {
            $successType = $null
            $commandMatch = [regex]::Match($content, 'new\s+(?<name>\w+(?:Command|Query))\(')
            if ($commandMatch.Success) {
                $commandName = $commandMatch.Groups['name'].Value
                $commandType = Resolve-TypeInfo -TypeExpression $commandName -TypeIndex $TypeIndex -PreferredNamespaces $usingNamespaces
                if ($null -ne $commandType) {
                    foreach ($base in $commandType.Bases) {
                        if ($base -match 'I(?:Command|Query)<(?<result>.+)>') {
                            $successType = $matches['result'].Trim()
                            break
                        }
                    }
                }
            }

            $status = if ($content -match 'ToCreatedHttpResult\(\)' -or $verb -eq "POST") { 201 } else { 200 }
            $responses.Add([pscustomobject]@{ StatusCode = $status; Type = $successType; Kind = "success" })
        }
    }

    return [pscustomobject]@{
        ClassName = $endpointClassName
        FilePath = $FilePath
        Method = $verb
        Path = $urlPath
        Tag = $tag
        AuthRequirement = $authRequirement
        Permission = $permission
        HasIdempotency = $hasIdempotency
        ExcludedFromDescription = $excludeFromDescription
        RequestType = $requestType
        QueryType = $queryType
        PathParameters = $pathParams.ToArray()
        Responses = $responses.ToArray()
        NestedTypes = $nestedTypeMap
        UsingNamespaces = $usingNamespaces
    }
}

function Get-HubMap {
    param([string]$ApiRoot)

    $hubs = New-Object System.Collections.Generic.List[object]
    foreach ($file in (Get-ChildItem -Path (Join-Path $ApiRoot 'Hubs') -Filter *.cs)) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($content -notmatch '\[SignalRHub\("(?<path>[^"]+)"') {
            continue
        }

        $path = $matches['path']
        $hubNameMatch = [regex]::Match($content, 'public\s+sealed\s+class\s+(\w+)')
        if (-not $hubNameMatch.Success) {
            continue
        }

        $hubName = $hubNameMatch.Groups[1].Value
        $authorize = $content -match '\[Authorize'
        $methods = New-Object System.Collections.Generic.List[object]
        foreach ($match in [regex]::Matches($content, '(?ms)(?:\[HasPermission\((?<permission>[^\]]+)\)\]\s*)?public\s+async\s+Task(?:<(?<returnType>[\w<>\.\?,\s]+)>)?\s+(?<name>\w+)\((?<params>.*?)\)\s*\{')) {
            $name = $match.Groups['name'].Value
            if ($name -in @('OnConnectedAsync', 'OnDisconnectedAsync')) {
                continue
            }

            $parameters = New-Object System.Collections.Generic.List[object]
            foreach ($token in (Split-TopLevel -InputText $match.Groups['params'].Value)) {
                $field = Parse-ParameterToken -Token $token
                if ($null -ne $field) {
                    $parameters.Add($field)
                }
            }

            $methods.Add([pscustomobject]@{
                Name = $name
                Parameters = $parameters.ToArray()
                ReturnType = $match.Groups['returnType'].Value.Trim()
                Permission = $match.Groups['permission'].Value.Trim()
            })
        }

        $clientInterface = switch ($hubName) {
            "AuctionHub" { "src/core/OIO.Application/Context/AuctionContext/Hubs/IAuctionHubClient.cs" }
            "DisputeHub" { "src/core/OIO.Application/Context/ModerationContext/Hubs/IDisputeHubClient.cs" }
            "NotificationHub" { "src/core/OIO.Application/Context/NotificationContext/Hubs/INotificationHubClient.cs" }
            default { $null }
        }

        $events = New-Object System.Collections.Generic.List[object]
        if ($clientInterface) {
            $clientPath = Join-Path $RepoRoot $clientInterface
            $clientContent = Get-Content -LiteralPath $clientPath -Raw
            foreach ($match in [regex]::Matches($clientContent, 'Task\s+(?<name>\w+)\((?<params>.*?)\);')) {
                $eventName = $match.Groups['name'].Value
                $payloadType = ""
                $paramsText = $match.Groups['params'].Value.Trim()
                if ($paramsText -match '^(?<type>.+?)\s+\w+$') {
                    $payloadType = $matches['type'].Trim()
                }
                elseif ([string]::IsNullOrWhiteSpace($paramsText)) {
                    $payloadType = ""
                }

                $events.Add([pscustomobject]@{
                    Name = $eventName
                    PayloadType = $payloadType
                })
            }
        }

        $hubs.Add([pscustomobject]@{
            Name = $hubName
            Path = $path
            NegotiatePath = ($path.TrimStart('/') + "/negotiate?negotiateVersion=1")
            Authorize = $authorize
            Methods = $methods.ToArray()
            Events = $events.ToArray()
        })
    }

    return $hubs.ToArray()
}

function Get-SchemaDisplayType {
    param([string]$TypeName)

    if ([string]::IsNullOrWhiteSpace($TypeName)) {
        return "none"
    }

    return $TypeName.Trim()
}

function Add-SchemaReference {
    param(
        [string]$TypeExpression,
        [hashtable]$SchemaSet
    )

    if ([string]::IsNullOrWhiteSpace($TypeExpression)) {
        return
    }

    $normalized = $TypeExpression.Trim()
    if (Is-GenericPlaceholder -TypeName $normalized) {
        return
    }

    $SchemaSet[$normalized] = $true
}

function Collect-SchemaReferences {
    param(
        [object[]]$Endpoints,
        [object[]]$Hubs
    )

    $schemas = @{}
    foreach ($endpoint in $Endpoints) {
        if ($null -ne $endpoint.RequestType) {
            Add-SchemaReference -TypeExpression $endpoint.RequestType.FullName -SchemaSet $schemas
        }

        if ($null -ne $endpoint.QueryType) {
            Add-SchemaReference -TypeExpression $endpoint.QueryType.FullName -SchemaSet $schemas
        }

        foreach ($response in ($endpoint.Responses | Where-Object { $_.Type })) {
            Add-SchemaReference -TypeExpression $response.Type -SchemaSet $schemas
        }
    }

    foreach ($hub in $Hubs) {
        foreach ($method in $hub.Methods) {
            foreach ($parameter in $method.Parameters) {
                Add-SchemaReference -TypeExpression $parameter.Type -SchemaSet $schemas
            }

            if ($method.ReturnType) {
                Add-SchemaReference -TypeExpression $method.ReturnType -SchemaSet $schemas
            }
        }

        foreach ($event in $hub.Events) {
            if ($event.PayloadType) {
                Add-SchemaReference -TypeExpression $event.PayloadType -SchemaSet $schemas
            }
        }
    }

    return $schemas
}

function Resolve-SchemaDependencies {
    param(
        [hashtable]$SchemaSet,
        [hashtable]$TypeIndex,
        [hashtable]$EndpointNestedTypes
    )

    $queue = New-Object System.Collections.Generic.Queue[string]
    foreach ($key in $SchemaSet.Keys) {
        $queue.Enqueue($key)
    }

    while ($queue.Count -gt 0) {
        $typeExpression = $queue.Dequeue()
        $primitive = Get-PrimitiveTypeName -TypeName $typeExpression
        if ($primitive) {
            continue
        }

        if (Is-GenericPlaceholder -TypeName $typeExpression) {
            continue
        }

        $collectionElement = Get-CollectionElementType -TypeName $typeExpression
        if ($collectionElement) {
            if (-not $SchemaSet.ContainsKey($collectionElement)) {
                $SchemaSet[$collectionElement] = $true
                $queue.Enqueue($collectionElement)
            }
            continue
        }

        if ($typeExpression -match '^(?<outer>\w+)<(?<inner>.+)>$') {
            $outer = $matches['outer']
            $innerTypes = Split-TopLevel -InputText $matches['inner']
            if (-not $SchemaSet.ContainsKey($outer)) {
                $SchemaSet[$outer] = $true
                $queue.Enqueue($outer)
            }
            foreach ($innerType in $innerTypes) {
                if (Is-GenericPlaceholder -TypeName $innerType) {
                    continue
                }
                if (-not $SchemaSet.ContainsKey($innerType)) {
                    $SchemaSet[$innerType] = $true
                    $queue.Enqueue($innerType)
                }
            }
            continue
        }

        $typeInfo = $null
        if ($EndpointNestedTypes.ContainsKey($typeExpression)) {
            $typeInfo = $EndpointNestedTypes[$typeExpression]
        }
        else {
            $typeInfo = Resolve-TypeInfo -TypeExpression $typeExpression -TypeIndex $TypeIndex -PreferredNamespaces @()
        }

        if ($null -eq $typeInfo) {
            continue
        }

        foreach ($field in $typeInfo.Fields) {
            $fieldType = $field.Type
            $basePrimitive = Get-PrimitiveTypeName -TypeName $fieldType
            if ($basePrimitive) {
                continue
            }
            if (Is-GenericPlaceholder -TypeName $fieldType) {
                continue
            }
            if (-not $SchemaSet.ContainsKey($fieldType)) {
                $SchemaSet[$fieldType] = $true
                $queue.Enqueue($fieldType)
            }
        }

        foreach ($base in $typeInfo.Bases) {
            if ($base -match '^I[A-Z]') {
                continue
            }
            if (Is-GenericPlaceholder -TypeName $base) {
                continue
            }
            if (-not $SchemaSet.ContainsKey($base)) {
                $SchemaSet[$base] = $true
                $queue.Enqueue($base)
            }
        }
    }
}

function Get-SchemaFieldsRecursive {
    param(
        [object]$TypeInfo,
        [hashtable]$TypeIndex,
        [hashtable]$EndpointNestedTypes,
        [System.Collections.Generic.HashSet[string]]$Visited
    )

    $key = $TypeInfo.FullName
    if ($Visited.Contains($key)) {
        return @()
    }
    $Visited.Add($key) | Out-Null

    $fields = New-Object System.Collections.Generic.List[object]

    foreach ($base in $TypeInfo.Bases) {
        if ($base -match '^I[A-Z]') {
            continue
        }

        $baseInfo = if ($EndpointNestedTypes.ContainsKey($base)) {
            $EndpointNestedTypes[$base]
        } else {
            Resolve-TypeInfo -TypeExpression $base -TypeIndex $TypeIndex -PreferredNamespaces @($TypeInfo.Namespace)
        }

        if ($null -ne $baseInfo) {
            foreach ($field in (Get-SchemaFieldsRecursive -TypeInfo $baseInfo -TypeIndex $TypeIndex -EndpointNestedTypes $EndpointNestedTypes -Visited $Visited)) {
                $fields.Add($field)
            }
        }
    }

    foreach ($field in $TypeInfo.Fields) {
        $existingFieldNames = $fields | ForEach-Object { $_.Name }
        if ($existingFieldNames -contains $field.Name) {
            continue
        }
        $fields.Add($field)
    }

    return $fields.ToArray()
}

function Format-FieldRequirement {
    param([object]$Field)

    if ($Field.Required) {
        return "required"
    }

    if ($Field.DefaultValue) {
        return "default = $($Field.DefaultValue)"
    }

    if ($Field.Type.Trim().EndsWith('?')) {
        return "optional"
    }

    $primitive = Get-PrimitiveTypeName -TypeName $Field.Type
    if ($primitive -in @("string", "guid", "datetime", "datetime-offset", "duration", "ip-address")) {
        return "optional"
    }

    return "required"
}

function Get-SchemaLink {
    param([string]$TypeExpression)

    $display = Get-SchemaDisplayType -TypeName $TypeExpression
    $anchor = ConvertTo-AnchorId -Value $display
    return "[$display](./schemas.md#$anchor)"
}

function Build-SchemaSection {
    param(
        [string]$TypeExpression,
        [hashtable]$TypeIndex,
        [hashtable]$EndpointNestedTypes
    )

    $display = Get-SchemaDisplayType -TypeName $TypeExpression
    $anchor = ConvertTo-AnchorId -Value $display
    $primitive = Get-PrimitiveTypeName -TypeName $TypeExpression
    if ($primitive) {
        return (
            @(
                ('<a id="{0}"></a>' -f $anchor)
                "## $display"
                ""
                "- Kieu du lieu co ban: $primitive"
                ""
            ) -join "`n"
        )
    }

    $collectionElement = Get-CollectionElementType -TypeName $TypeExpression
    if ($collectionElement) {
        $elementLink = Get-SchemaLink -TypeExpression $collectionElement
        return (
            @(
                ('<a id="{0}"></a>' -f $anchor)
                "## $display"
                ""
                "- Collection payload"
                "- Phan tu: $elementLink"
                ""
            ) -join "`n"
        )
    }

    if ($TypeExpression -match '^(?<outer>\w+)<(?<inner>.+)>$') {
        $outer = $matches['outer']
        $inner = Split-TopLevel -InputText $matches['inner']
        if ($outer -eq "PagedList" -and @($inner).Count -eq 1) {
            $itemLink = Get-SchemaLink -TypeExpression $inner[0]
            $metadataLink = Get-SchemaLink -TypeExpression "Metadata"
            return (
                @(
                    ('<a id="{0}"></a>' -f $anchor)
                    "## $display"
                    ""
                    "| Field | Type | Mo ta |"
                    "| --- | --- | --- |"
                    "| items | $itemLink[] | Danh sach phan tu trong trang hien tai |"
                    "| metadata | $metadataLink | Thong tin paging |"
                    ""
                ) -join "`n"
            )
        }

        if ($outer -eq "HubCommandResult" -and @($inner).Count -eq 1) {
            $dataLink = Get-SchemaLink -TypeExpression $inner[0]
            $errorLink = Get-SchemaLink -TypeExpression "ErrorNotification"
            return (
                @(
                    ('<a id="{0}"></a>' -f $anchor)
                    "## $display"
                    ""
                    "| Field | Type | Mo ta |"
                    "| --- | --- | --- |"
                    "| success | bool | Ket qua thuc thi command realtime |"
                    "| data | $dataLink | Payload thanh cong |"
                    "| error | $errorLink | Thong tin loi neu success = false |"
                    ""
                ) -join "`n"
            )
        }
    }

    if ($TypeExpression -in @("ProblemDetails", "ValidationProblemDetails")) {
        $rows = @(
            "| Field | Type | Ghi chu |"
            "| --- | --- | --- |"
            "| title | string | Tieu de loi |"
            "| status | number | HTTP status code |"
            "| detail | string | Noi dung chi tiet |"
            "| instance | string | Route hoac request instance |"
        )

        if ($TypeExpression -eq "ValidationProblemDetails") {
            $rows += "| errors | [Dictionary<string, string[]>](./schemas.md#schema-dictionary-string-string) | Tap loi validation theo field |"
        }

        return (
            @(
                ('<a id="{0}"></a>' -f $anchor)
                "## $display"
                ""
                ($rows -join "`n")
                ""
            ) -join "`n"
        )
    }

    if ($TypeExpression -match '^Dictionary<string,\s*string\[\]>\??$') {
        return (
            @(
                ('<a id="{0}"></a>' -f $anchor)
                "## $display"
                ""
                "- Kieu map string -> string[] dung cho validation errors hoac metadata tuong tu."
                ""
            ) -join "`n"
        )
    }

    $typeInfo = if ($EndpointNestedTypes.ContainsKey($TypeExpression)) {
        $EndpointNestedTypes[$TypeExpression]
    } else {
        Resolve-TypeInfo -TypeExpression $TypeExpression -TypeIndex $TypeIndex -PreferredNamespaces @()
    }

    if ($null -eq $typeInfo) {
        return (
            @(
                ('<a id="{0}"></a>' -f $anchor)
                "## $display"
                ""
                "- Khong tim thay source type trong repo. Can cap nhat generator neu day la type moi."
                ""
            ) -join "`n"
        )
    }

    if ($typeInfo.Kind -eq "enum") {
        $values = [regex]::Matches($typeInfo.Body, '(?m)^\s*(\w+)\s*(?:=\s*[^,]+)?\s*,?') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -and $_ -ne "public" }
        $items = ($values | ForEach-Object { "- $_" }) -join "`n"
        return (
            @(
                ('<a id="{0}"></a>' -f $anchor)
                "## $display"
                ""
                $items
                ""
            ) -join "`n"
        )
    }

    $fields = Get-SchemaFieldsRecursive -TypeInfo $typeInfo -TypeIndex $TypeIndex -EndpointNestedTypes $EndpointNestedTypes -Visited ([System.Collections.Generic.HashSet[string]]::new())
    $rows = foreach ($field in $fields) {
        $typeLabel = $field.Type
        $primitiveLabel = Get-PrimitiveTypeName -TypeName $field.Type
        $elementType = Get-CollectionElementType -TypeName $field.Type
        if ($primitiveLabel) {
            $typeLabel = $primitiveLabel
        }
        elseif ($elementType) {
            $typeLabel = "$(Get-SchemaLink -TypeExpression $elementType)`[]"
        }
        elseif ($field.Type -match '^(?:PagedList|HubCommandResult)<.+>$') {
            $typeLabel = Get-SchemaLink -TypeExpression $field.Type
        }
        else {
            $typeLabel = Get-SchemaLink -TypeExpression $field.Type
        }

        "| ``$($field.Name)`` | $typeLabel | $(Format-FieldRequirement -Field $field) |"
    }

    $sourceRelative = Resolve-Path -LiteralPath $typeInfo.FilePath -Relative
    $table = if (@($rows).Count -gt 0) {
        @("| Field | Type | Ghi chu |", "| --- | --- | --- |") + $rows -join "`n"
    }
    else {
        "- Khong co field cong khai duoc parser."
    }

    return (
        @(
            ('<a id="{0}"></a>' -f $anchor)
            "## $display"
            ""
            "- Source: $sourceRelative"
            ""
            $table
            ""
        ) -join "`n"
    )
}

function Get-EndpointDocBucket {
    param([object]$Endpoint)

    $path = $Endpoint.Path
    if ($path.StartsWith("api/payments") -or $path.StartsWith("api/admin/payments") -or $path.StartsWith("api/orders")) {
        return "payment-order"
    }
    if ($path.StartsWith("api/reports") -or $path.StartsWith("api/disputes") -or $path.StartsWith("api/notifications") -or $path.StartsWith("api/admin/") -or $path.StartsWith("api/warehouse") -or $path.StartsWith("webhooks/") -or $path -eq "api/me/reports") {
        return "moderation-warehouse"
    }
    if ($path.StartsWith("api/auth/") -or $path.StartsWith("api/terms") -or $path.StartsWith("api/me/")) {
        return "user"
    }
    if ($path.StartsWith("api/categories") -or $path.StartsWith("api/items") -or $path.StartsWith("api/sellers") -or $path.StartsWith("api/auctions")) {
        return "auction"
    }
    return "moderation-warehouse"
}

function Build-EndpointSection {
    param(
        [object]$Endpoint,
        [hashtable]$Descriptions
    )

    $key = "$($Endpoint.Method) $($Endpoint.Path)"
    if (-not $Descriptions.http.ContainsKey($key)) {
        throw "Missing HTTP description override for '$key'."
    }

    $entry = $Descriptions.http[$key]
    if ([string]::IsNullOrWhiteSpace($entry.purpose)) {
        throw "Missing purpose for HTTP endpoint '$key'."
    }

    $builder = New-Object System.Text.StringBuilder
    [void]$builder.AppendLine("### $($Endpoint.Method) /$($Endpoint.Path)")
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("- Muc dich: $($entry.purpose)")
    [void]$builder.AppendLine("- Audience: $($entry.audience)")

    $authLine = switch ($Endpoint.AuthRequirement) {
        "anonymous" { "AllowAnonymous" }
        "authorized" {
            if ([string]::IsNullOrWhiteSpace($Endpoint.Permission)) {
                "Bearer token"
            } else {
                "Bearer token + permission $($Endpoint.Permission)"
            }
        }
        default { "Bearer token" }
    }
    [void]$builder.AppendLine("- Auth: $authLine")

    $headers = New-Object System.Collections.Generic.List[string]
    if ($Endpoint.AuthRequirement -ne "anonymous") {
        $headers.Add("Authorization: Bearer <token>")
    }
    if ($Endpoint.HasIdempotency) {
        $headers.Add("Idempotency-Key: <unique-key> (required)")
    }
    if ($headers.Count -eq 0) {
        $headers.Add("Khong co header dac biet")
    }
    [void]$builder.AppendLine("- Headers: " + ($headers -join ", "))

    if ($Endpoint.PathParameters.Count -gt 0) {
        [void]$builder.AppendLine("- Path params:")
        foreach ($param in $Endpoint.PathParameters) {
            [void]$builder.AppendLine("  - $($param.Name): $($param.Type)")
        }
    }
    else {
        [void]$builder.AppendLine("- Path params: none")
    }

    if ($null -ne $Endpoint.QueryType) {
        $queryLink = Get-SchemaLink -TypeExpression $Endpoint.QueryType.FullName
        [void]$builder.AppendLine("- Query params: $queryLink")
    }
    else {
        [void]$builder.AppendLine("- Query params: none")
    }

    if ($null -ne $Endpoint.RequestType) {
        $requestLink = Get-SchemaLink -TypeExpression $Endpoint.RequestType.FullName
        [void]$builder.AppendLine("- Request body: $requestLink")
    }
    else {
        [void]$builder.AppendLine("- Request body: none")
    }

    $successResponses = $Endpoint.Responses | Where-Object { $_.Kind -eq "success" } | Sort-Object StatusCode
    [void]$builder.AppendLine("- Success responses:")
    foreach ($response in $successResponses) {
        $typePart = if ($response.Type) { Get-SchemaLink -TypeExpression $response.Type } else { "none" }
        [void]$builder.AppendLine("  - $(Get-StatusLabel -StatusCode $response.StatusCode): $typePart")
    }

    $errorResponses = $Endpoint.Responses | Where-Object { $_.Kind -eq "error" } | Sort-Object StatusCode -Unique
    if (@($errorResponses).Count -gt 0) {
        $statusLabels = $errorResponses | ForEach-Object { Get-StatusLabel -StatusCode $_.StatusCode }
        [void]$builder.AppendLine("- Error statuses: " + ($statusLabels -join ", "))
    }
    else {
        [void]$builder.AppendLine("- Error statuses: none documented")
    }

    $notes = New-Object System.Collections.Generic.List[string]
    if ($Endpoint.HasIdempotency) {
        $notes.Add("Endpoint nay duoc gate boi Idempotency filter.")
    }
    if ($Endpoint.ExcludedFromDescription) {
        $notes.Add("Route nay dang bi an khoi Swagger/Scalar va duoc document rieng.")
    }
    if (-not [string]::IsNullOrWhiteSpace($entry.notes)) {
        $notes.Add($entry.notes)
    }
    if ($notes.Count -gt 0) {
        [void]$builder.AppendLine("- Notes:")
        foreach ($note in $notes) {
            [void]$builder.AppendLine("  - $note")
        }
    }

    [void]$builder.AppendLine("")
    return $builder.ToString()
}

function Build-HubSection {
    param(
        [object]$Hub,
        [hashtable]$Descriptions
    )

    $builder = New-Object System.Text.StringBuilder
    [void]$builder.AppendLine("## $($Hub.Name)")
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("- URL: /$($Hub.Path.TrimStart('/'))")
    [void]$builder.AppendLine("- Negotiate: POST /$($Hub.NegotiatePath)")
    [void]$builder.AppendLine("- Auth: " + ($(if ($Hub.Authorize) { "Bearer token required" } else { "anonymous" })))
    [void]$builder.AppendLine("")
    [void]$builder.AppendLine("### Client -> Server methods")
    [void]$builder.AppendLine("")
    foreach ($method in $Hub.Methods) {
        $key = "$($Hub.Name).$($method.Name)"
        if (-not $Descriptions.signalr_methods.ContainsKey($key)) {
            throw "Missing SignalR method description override for '$key'."
        }
        $entry = $Descriptions.signalr_methods[$key]
        if ([string]::IsNullOrWhiteSpace($entry.purpose)) {
            throw "Missing purpose for SignalR method '$key'."
        }
        [void]$builder.AppendLine("#### $($method.Name)")
        [void]$builder.AppendLine("")
        [void]$builder.AppendLine("- Muc dich: $($entry.purpose)")
        [void]$builder.AppendLine("- Audience: $($entry.audience)")
        if ($method.Permission) {
            [void]$builder.AppendLine("- Permission: $($method.Permission)")
        }
        if ($method.Parameters.Count -gt 0) {
            [void]$builder.AppendLine("- Parameters:")
            foreach ($parameter in $method.Parameters) {
                $primitive = Get-PrimitiveTypeName -TypeName $parameter.Type
                if ($primitive) {
                    $typeValue = $primitive
                }
                else {
                    $typeValue = Get-SchemaLink -TypeExpression $parameter.Type
                }
                $requirement = Format-FieldRequirement -Field $parameter
                [void]$builder.AppendLine("  - $($parameter.Name): $typeValue ($requirement)")
            }
        }
        else {
            [void]$builder.AppendLine("- Parameters: none")
        }

        if ($method.ReturnType) {
            [void]$builder.AppendLine("- Return: $(Get-SchemaLink -TypeExpression $method.ReturnType)")
        }
        else {
            [void]$builder.AppendLine("- Return: none")
        }

        if (-not [string]::IsNullOrWhiteSpace($entry.notes)) {
            [void]$builder.AppendLine("- Notes: $($entry.notes)")
        }
        [void]$builder.AppendLine("")
    }

    [void]$builder.AppendLine("### Server -> Client events")
    [void]$builder.AppendLine("")
    foreach ($event in $Hub.Events) {
        $key = "$($Hub.Name).$($event.Name)"
        if (-not $Descriptions.signalr_events.ContainsKey($key)) {
            throw "Missing SignalR event description override for '$key'."
        }
        $entry = $Descriptions.signalr_events[$key]
        if ([string]::IsNullOrWhiteSpace($entry.purpose)) {
            throw "Missing purpose for SignalR event '$key'."
        }
        [void]$builder.AppendLine("#### $($event.Name)")
        [void]$builder.AppendLine("")
        [void]$builder.AppendLine("- Muc dich: $($entry.purpose)")
        [void]$builder.AppendLine("- Audience: $($entry.audience)")
        if ($event.PayloadType) {
            [void]$builder.AppendLine("- Payload: $(Get-SchemaLink -TypeExpression $event.PayloadType)")
        }
        else {
            [void]$builder.AppendLine("- Payload: none")
        }

        if (-not [string]::IsNullOrWhiteSpace($entry.notes)) {
            [void]$builder.AppendLine("- Notes: $($entry.notes)")
        }
        [void]$builder.AppendLine("")
    }

    return $builder.ToString()
}

$apiRoot = Join-Path $RepoRoot 'src/presentation/OIO.Api'
$applicationRoot = Join-Path $RepoRoot 'src/core/OIO.Application'
$domainRoot = Join-Path $RepoRoot 'src/core/OIO.Domain'
$docsRoot = Join-Path $RepoRoot 'docs'
$apiDocsRoot = Join-Path $docsRoot 'api'
$descriptionsPath = Join-Path $apiDocsRoot 'descriptions.yaml'

Ensure-Directory -Path $docsRoot
Ensure-Directory -Path $apiDocsRoot
Ensure-Directory -Path (Join-Path $RepoRoot 'scripts')

$typeIndex = Build-TypeIndex -SourceRoots @($applicationRoot, $domainRoot, $apiRoot)
$urlMap = Resolve-UrlConstants -FilePath (Join-Path $apiRoot 'Common/ApiEndpoint.Url.cs')

$endpoints = New-Object System.Collections.Generic.List[object]
$allNestedTypes = @{}
foreach ($endpointFile in (Get-ChildItem -Path (Join-Path $apiRoot 'Endpoints') -Recurse -Filter *.cs | Sort-Object FullName)) {
    $endpoint = Parse-EndpointFile -FilePath $endpointFile.FullName -UrlMap $urlMap -TypeIndex $typeIndex
    if ($null -eq $endpoint) {
        continue
    }

    foreach ($nestedKey in $endpoint.NestedTypes.Keys) {
        $allNestedTypes[$endpoint.NestedTypes[$nestedKey].FullName] = $endpoint.NestedTypes[$nestedKey]
        if (-not $allNestedTypes.ContainsKey($endpoint.NestedTypes[$nestedKey].Name)) {
            $allNestedTypes[$endpoint.NestedTypes[$nestedKey].Name] = $endpoint.NestedTypes[$nestedKey]
        }
    }
    $endpoints.Add($endpoint)
}

$hubs = Get-HubMap -ApiRoot $apiRoot
$descriptions = Load-Descriptions -Path $descriptionsPath
if ($BootstrapDescriptions) {
    $descriptions = @{
        http = @{}
        signalr_methods = @{}
        signalr_events = @{}
    }
}

foreach ($endpoint in $endpoints) {
    $key = "$($endpoint.Method) $($endpoint.Path)"
    if (-not $descriptions.http.ContainsKey($key) -or $BootstrapDescriptions) {
        $descriptions.http[$key] = @{
            purpose = Infer-EndpointPurpose -Name $endpoint.ClassName -Method $endpoint.Method -Path "/$($endpoint.Path)"
            audience = Infer-Audience -Path $endpoint.Path -AuthRequirement $endpoint.AuthRequirement -Permission $endpoint.Permission
            notes = switch ($endpoint.Path) {
                "webhooks/ghn" { "Webhook carrier tu GHN. Luon tra 200 de tranh retry vo han." }
                "api/payments/vnpay/return" { "Return URL cho browser redirect sau thanh toan VNPay." }
                "api/payments/vnpay/ipn" { "IPN server-to-server tu VNPay." }
                default { "" }
            }
        }
    }
}

foreach ($hub in $hubs) {
    foreach ($method in $hub.Methods) {
        $key = "$($hub.Name).$($method.Name)"
        if (-not $descriptions.signalr_methods.ContainsKey($key) -or $BootstrapDescriptions) {
            $descriptions.signalr_methods[$key] = @{
                purpose = Infer-HubPurpose -HubName $hub.Name -MemberName $method.Name -Kind "method"
                audience = "authenticated"
                notes = if ($method.Name -eq "PlaceBid") { "Method nay duoc gate boi idempotency hub filter." } else { "" }
            }
        }
    }
    foreach ($event in $hub.Events) {
        $key = "$($hub.Name).$($event.Name)"
        if (-not $descriptions.signalr_events.ContainsKey($key) -or $BootstrapDescriptions) {
            $descriptions.signalr_events[$key] = @{
                purpose = Infer-HubPurpose -HubName $hub.Name -MemberName $event.Name -Kind "event"
                audience = "subscribers"
                notes = if ($event.Name -eq "PriceUpdated") { "Contract da ton tai nhung co the chua duoc publish trong runtime." } else { "" }
            }
        }
    }
}

Save-Descriptions -Path $descriptionsPath -Descriptions $descriptions

$schemaRefs = Collect-SchemaReferences -Endpoints $endpoints.ToArray() -Hubs $hubs
Resolve-SchemaDependencies -SchemaSet $schemaRefs -TypeIndex $typeIndex -EndpointNestedTypes $allNestedTypes
foreach ($schemaKey in @($schemaRefs.Keys)) {
    if (Is-GenericPlaceholder -TypeName $schemaKey) {
        $schemaRefs.Remove($schemaKey)
    }
}

$schemaSections = foreach ($typeName in ($schemaRefs.Keys | Sort-Object)) {
    Build-SchemaSection -TypeExpression $typeName -TypeIndex $typeIndex -EndpointNestedTypes $allNestedTypes
}

$unresolvedSchemas = @($schemaSections | Where-Object { $_ -match 'Khong tim thay source type trong repo' })
if ($unresolvedSchemas.Count -gt 0) {
    $unresolvedNames = foreach ($section in $unresolvedSchemas) {
        if ($section -match '##\s+([^\r\n]+)') {
            $matches[1]
        }
    }
    throw "Generator con schema chua resolve duoc: $($unresolvedNames -join ', '). Hay cap nhat scripts/generate-api-docs.ps1 de map type moi."
}

$docsByBucket = @{
    "user" = New-Object System.Collections.Generic.List[object]
    "auction" = New-Object System.Collections.Generic.List[object]
    "payment-order" = New-Object System.Collections.Generic.List[object]
    "moderation-warehouse" = New-Object System.Collections.Generic.List[object]
}

foreach ($endpoint in ($endpoints | Sort-Object Path, Method)) {
    $bucket = Get-EndpointDocBucket -Endpoint $endpoint
    $docsByBucket[$bucket].Add($endpoint)
}

$rootReadme = @'
# OIO

OIO la backend cho nen tang dau gia truc tuyen, gom cac module lon: user/auth, catalog item, auction realtime, payment-wallet-order, moderation/dispute/report, warehouse va notification.

## Kien truc

- src/core/OIO.Domain: domain model, aggregate, enum, value object
- src/core/OIO.Application: command/query, DTO, service, event handler
- src/infrastructure/OIO.Infrastructure: persistence, provider, settings, integration
- src/presentation/OIO.Api: HTTP API, SignalR hub, OpenAPI/Scalar

## Chay local

1. Cap nhat .env neu can.
2. Khoi dong dependency bang compose.yaml va compose.override.yaml.
3. Chay API tu src/presentation/OIO.Api.
4. Trong development, OpenAPI duoc map tai /openapi/v1.json va Scalar tai /docs.

## Auth conventions

- API mac dinh dung Bearer JWT.
- Route api/admin/* danh cho admin.
- Mot so route POST duoc gate boi Idempotency-Key.
- SignalR hubs deu can auth va co them permission/check o tung method khi can.

## Tai lieu API

- [API index](./docs/api/README.md)
- [User + Auth](./docs/api/user.md)
- [Auction + Catalog](./docs/api/auction.md)
- [Payment + Order](./docs/api/payment-order.md)
- [Moderation + Warehouse](./docs/api/moderation-warehouse.md)
- [SignalR](./docs/api/signalr.md)
- [Schemas appendix](./docs/api/schemas.md)

## Regenerate docs

- Chay scripts/generate-api-docs.ps1 de regenerate docs tu source hien tai.
- Lan dau hoac khi can bootstrap descriptions.yaml, chay scripts/generate-api-docs.ps1 -BootstrapDescriptions.

'@
Set-Content -LiteralPath (Join-Path $RepoRoot 'README.md') -Value $rootReadme -Encoding UTF8

$apiIndex = @'
# API Reference

Tai lieu nay la reference chinh cho public surface cua OIO, bao gom HTTP API, callback/webhook va SignalR.

## Quy uoc chung

- Auth: mac dinh la Authorization: Bearer <token> neu route khong AllowAnonymous.
- Error format: route dung ProblemDetails / ValidationProblemDetails cho loi validation, auth, permission, state transition.
- Paging: cac endpoint list dung PagedList<T> se tra items va metadata.
- Timestamp: uu tien DateTime / DateTimeOffset nhu trong source.
- Currency: giu nguyen field currency theo payload runtime.
- Idempotency: cac route co IdempotencyFilter yeu cau header Idempotency-Key.
- Webhook/callback: VNPay va GHN la provider-driven route; contract co the khac flow UI thong thuong.

## Muc luc

- [User + Auth](./user.md)
- [Auction + Catalog](./auction.md)
- [Payment + Order](./payment-order.md)
- [Moderation + Warehouse](./moderation-warehouse.md)
- [SignalR](./signalr.md)
- [Schemas appendix](./schemas.md)

## Tooling

- Generator: scripts/generate-api-docs.ps1
- Generator se fail neu co endpoint hoac hub method moi chua co purpose trong descriptions.yaml.

'@
Set-Content -LiteralPath (Join-Path $apiDocsRoot 'README.md') -Value $apiIndex -Encoding UTF8

$docHeaders = @{
    "user.md" = "# User + Auth`n`nBao gom Auth, Terms, Me, wallet user-facing, verification va seller profile.`n`n"
    "auction.md" = "# Auction + Catalog`n`nBao gom Categories, Items, Sellers va Auctions.`n`n"
    "payment-order.md" = "# Payment + Order`n`nBao gom Payments, Wallet, VNPay, Orders/Returns va admin payment ops.`n`n"
    "moderation-warehouse.md" = "# Moderation + Warehouse`n`nBao gom Reports, Disputes, admin ops, Notifications HTTP, Warehouse va Webhooks.`n`n"
}

foreach ($pair in $docsByBucket.GetEnumerator()) {
    $fileName = switch ($pair.Key) {
        "user" { "user.md" }
        "auction" { "auction.md" }
        "payment-order" { "payment-order.md" }
        default { "moderation-warehouse.md" }
    }

    $builder = New-Object System.Text.StringBuilder
    [void]$builder.Append($docHeaders[$fileName])
    foreach ($endpoint in $pair.Value) {
        [void]$builder.Append((Build-EndpointSection -Endpoint $endpoint -Descriptions $descriptions))
    }

    Set-Content -LiteralPath (Join-Path $apiDocsRoot $fileName) -Value $builder.ToString() -Encoding UTF8
}

$signalrBuilder = New-Object System.Text.StringBuilder
[void]$signalrBuilder.AppendLine("# SignalR")
[void]$signalrBuilder.AppendLine("")
[void]$signalrBuilder.AppendLine("Bao gom ca hub path, negotiate endpoint, client -> server methods va server -> client events.")
[void]$signalrBuilder.AppendLine("")
foreach ($hub in $hubs) {
    [void]$signalrBuilder.Append((Build-HubSection -Hub $hub -Descriptions $descriptions))
}
Set-Content -LiteralPath (Join-Path $apiDocsRoot 'signalr.md') -Value $signalrBuilder.ToString() -Encoding UTF8

$schemasHeader = @"
# Schemas appendix

File nay gom request/response/DTO duoc tham chieu tu HTTP API va SignalR docs.

"@
Set-Content -LiteralPath (Join-Path $apiDocsRoot 'schemas.md') -Value ($schemasHeader + ($schemaSections -join "`n")) -Encoding UTF8

$missingPurposes = @(
    $descriptions.http.GetEnumerator() | Where-Object { [string]::IsNullOrWhiteSpace($_.Value.purpose) } | ForEach-Object { "HTTP: $($_.Key)" }
    $descriptions.signalr_methods.GetEnumerator() | Where-Object { [string]::IsNullOrWhiteSpace($_.Value.purpose) } | ForEach-Object { "SignalR method: $($_.Key)" }
    $descriptions.signalr_events.GetEnumerator() | Where-Object { [string]::IsNullOrWhiteSpace($_.Value.purpose) } | ForEach-Object { "SignalR event: $($_.Key)" }
)

if (@($missingPurposes).Count -gt 0) {
    throw "Descriptions file is missing purpose for:`n$($missingPurposes -join "`n")"
}

Write-Host "Generated README and API docs."
Write-Host "HTTP endpoints documented: $($endpoints.Count)"
Write-Host "SignalR hubs documented: $($hubs.Count)"
