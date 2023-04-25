using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;

public class DebugLoggerConfig : SerializedScriptableObject
{
	[EnumFlag]
	[SerializeField] public LogContext EnabledContexts;
	[EnumFlag]
	[SerializeField] public LogPriority EnabledPriorities;

	[Space]
	[SerializeField] public bool LogMessages;
	[SerializeField] public bool LogWarnings;
	[SerializeField] public bool LogErrors;
	[SerializeField] public bool LogAssertions;
	[SerializeField] public bool LogExceptions;
}