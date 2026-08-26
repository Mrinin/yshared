using UnityEngine;

namespace YShared.Console
{
    public enum ObjectFindType
    {
        /// <summary>
        /// Finds all objects with this class and calls this method on all of them. Internally uses <c>GameObject.FindObjectsByType</c>.
        /// </summary>
        All, 
        /// <summary>
        /// Finds any object with this class and calls this method on it. Internally uses <c>GameObject.FindAnyByType</c>.
        /// </summary>
        Any, 
        /// <summary>
        /// Finds the first object with this class and calls this method on it. Internally uses <c>GameObject.FindFirstByType</c>.
        /// </summary>
        First,
        /// <summary>
        /// <para> The first argument of this command will be the instance to run this command on. </para>
        /// <para> Note: You do not need to make the first argument of this method this class, but if you do it will be automatically filled in. </para>
        /// </summary>
        Argument,
        /// <summary>
        /// <para> The first argument of this command will be the array of instances to run this command on. </para>
        /// <para> Note: You do not need to make the first argument of this method an array of this class, but if you do it will be automatically filled in. </para>
        /// </summary>
        ArgumentArray
    }

}