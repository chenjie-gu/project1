# Player Y-Axis Movement Analysis - Hammer Jitter Issue

## Problem
The player jitters when standing on the hammer instead of moving smoothly with it.

## Y-Axis Movement Code Flow in FixedUpdate()

### 1. Initial Velocity Setup (Line 169)
```csharp
Vector2 newVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
```
- Sets X velocity from input
- **Preserves current Y velocity** from rigidbody

### 2. Wall Sliding (Lines 172-195)
```csharp
if (enableWallSliding && !isGrounded)
{
    if (isTouchingLeftWall && moveInput < 0)
    {
        newVelocity.y = -wallSlideSpeed;  // Overrides Y velocity
        newVelocity.x = 0;
    }
    // ... more wall sliding logic
}
```
- Only affects Y velocity when NOT grounded
- Shouldn't interfere with platform movement

### 3. Velocity Applied (Line 229)
```csharp
rb.linearVelocity = newVelocity;
```
- **This is where the velocity is set**
- At this point, Y velocity is either:
  - Preserved from previous frame (line 169)
  - Set by wall sliding (if applicable)

### 4. Jump Handling (Lines 231-239)
```csharp
if (jumpRequested)
{
    float jumpForceToUse = isFlattened ? flattenedJumpForce : jumpForce;
    if (jumpForceToUse > 0f)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForceToUse);
    }
    jumpRequested = false;
}
```
- Overrides Y velocity if jumping
- This happens AFTER platform detection should occur

### 5. Platform Velocity Detection (Lines 259-279) ⚠️ **THE PROBLEM**
```csharp
// Physics-based platform movement - no direct position manipulation
if (isGrounded && groundCheck != null)
{
    // Detect platform and apply its velocity to player
    Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, checkRadius, groundLayer);
    
    foreach (var hit in hits)
    {
        if ((groundLayer.value & (1 << hit.gameObject.layer)) != 0)
        {
            Rigidbody2D platformRb = hit.gameObject.GetComponent<Rigidbody2D>();
            if (platformRb != null)
            {
                // Apply platform velocity to player's velocity
                Vector2 platformVelocity = platformRb.linearVelocity;
                newVelocity += platformVelocity;  // ⚠️ MODIFIES newVelocity
                break;
            }
        }
    }
}
```

## THE BUG

**The platform velocity code modifies `newVelocity` AFTER `rb.linearVelocity = newVelocity` has already been set on line 229!**

This means:
1. Line 229: Velocity is applied to rigidbody
2. Lines 259-279: Platform velocity is calculated and added to `newVelocity`
3. **But `newVelocity` is never used again!** The platform velocity is calculated but never applied.

## Why This Causes Jittering

- The player's Y velocity doesn't match the hammer's Y velocity
- Physics tries to keep the player on the platform, but the velocities don't sync
- This creates a conflict: player wants to fall (gravity), platform wants to move up/down
- Result: Jittering as the player bounces slightly on the platform

## Solution

The platform velocity detection should happen **BEFORE** line 229, so the platform velocity is included when `rb.linearVelocity = newVelocity` is set.

### Current Order (WRONG):
1. Create newVelocity
2. Apply newVelocity to rigidbody
3. Calculate platform velocity (but never use it)

### Correct Order:
1. Create newVelocity
2. **Calculate and add platform velocity to newVelocity**
3. Apply newVelocity to rigidbody

