# Introduction

Welcome to **YShared**! This is a basic example page demonstrating the various Markdown features supported by the documentation.

## Headings

Markdown supports multiple heading levels:

# Heading 1
## Heading 2
### Heading 3
#### Heading 4
##### Heading 5
###### Heading 6

---

## Text Formatting

You can use **bold text**, *italic text*, and ***bold italic text***.

You can also use ~~strikethrough text~~.

You can highlight `inline code` using backticks.

You can combine formatting, such as **bold with `inline code`**.

---

## Links

You can create links to other pages:

[Getting Started](getting-started.md)

You can also link to external websites:

[Unity](https://unity.com/)

---

## Images

Images can be embedded using:

![YShared Logo](images/logo.png)

You can also add alternative text describing the image.

---

## Lists

### Unordered List

- Item one
- Item two
- Item three
  - Nested item
  - Another nested item
    - Even deeper

### Ordered List

1. First item
2. Second item
3. Third item
   1. Nested item
   2. Another nested item

### Task List

- [x] Completed task
- [ ] Incomplete task
- [ ] Another task

---

## Blockquotes

> This is a blockquote.
>
> It can span multiple lines.
>
> > Blockquotes can also be nested.

---

## Code

Inline code:

`Debug.Log("Hello World");`

### C# Code Block

```csharp
using UnityEngine;

public class Example : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("Hello World!");
    }
}
```

Although....