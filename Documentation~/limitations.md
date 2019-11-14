# Known Limitations

When instantiating a scene containing cloneable assets, the event firing sequence allows a window (when OnValidate is triggered) where user code can be executed BEFORE asset remapping has happened. 

See [Scene Template Instantiation Sequence](api.md#scene-template-instantiation-sequence) for more information