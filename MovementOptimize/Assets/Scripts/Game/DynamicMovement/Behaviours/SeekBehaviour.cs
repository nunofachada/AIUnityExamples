﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * Author: Nuno Fachada
 * */
 using UnityEngine;

public class SeekBehaviour : SteeringBehaviour
{

    // Seek behaviour
    public override SteeringOutput GetSteering(GameObject target)
    {
        // Initialize linear and angular forces to zero
        Vector2 linear = Vector2.zero;
        float angular = 0f;

        // Do I have a target?
        if (target != null)
        {
            // Get the direction to the target
            linear = target.transform.position - transform.position;

            // Give full acceleration along this direction
            linear = linear.normalized * MaxAccel;

            // Seek behaviour does not set torque
            angular = 0f;
        }

        // Output the steering
        return new SteeringOutput(linear, angular);
    }
}
