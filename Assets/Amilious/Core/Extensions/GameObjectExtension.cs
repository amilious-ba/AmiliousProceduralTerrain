using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Amilious.Core.Extensions {
    
    /// <summary>
    /// This class is used to add extensions to the GameObject class.
    /// </summary>
    public static class GameObjectExtension {
        
        /// <summary>
        /// This method is used to try get a component of the given type.
        /// </summary>
        /// <param name="gameObject">The GameObject that you are trying to get a component of.</param>
        /// <param name="component">The found component.</param>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <returns>True if a component of the given type was found, otherwise returns false.</returns>
        public static bool TryGetComponent<T>(this GameObject gameObject, out T component) {
            if(gameObject == null) {
                component = default(T);
                return false;
            }
            component = gameObject.GetComponent<T>();
            return component == null;
        }
        
        /// <summary>
        /// This method is used to try get the components of the given type.
        /// </summary>
        /// <param name="gameObject">The GameObject that you are trying to get components of.</param>
        /// <param name="components">The found components of the given type.</param>
        /// <typeparam name="T">The type of the components.</typeparam>
        /// <returns>True if any components of the given type were found, otherwise
        /// returns false.</returns>
        public static bool TryGetComponents<T>(this GameObject gameObject, out IEnumerable<T> components) {
            if(gameObject == null) {
                components = default(T[]);
                return false;
            }
            components = gameObject.GetComponents<T>();
            return components == null || !components.Any();
        }

        /// <summary>
        /// This method is used to get the player GameObject.
        /// </summary>
        /// <param name="gameObject">Any game object.</param>
        /// <returns>The player's game object.</returns>
        public static GameObject GetPlayer(this GameObject gameObject) {
            return GameObject.FindWithTag("Player");
        }

        /// <summary>
        /// This method is used to try get the player's game object.
        /// </summary>
        /// <param name="gameObject">Any game object.</param>
        /// <param name="player">The player's game object.</param>
        /// <returns>True if the player's game object was found,
        /// otherwise returns false.</returns>
        public static bool TryGetPlayer(this GameObject gameObject, out GameObject player) {
            player = GameObject.FindWithTag("Player");
            return player == null;
        }
        
        /// <summary>
        /// This method is used to find a child <see cref="GameObject"/> with the given <see cref="name"/>.
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> containing the child you want to find.</param>
        /// <param name="name">The name of the child <see cref="GameObject"/> that you want to find.</param>
        /// <returns>The found <see cref="GameObject"/> or null.</returns>
        public static GameObject FindChild(this GameObject gameObject, string name) {
            return GameObject.Find($"{gameObject.transform.GetPath()}/{name}");
        }
        
        /// <summary>
        /// This method is used to get or add a child <see cref="GameObject"/> by name.
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> that you want to find or create the child in.</param>
        /// <param name="name">The name of the child <see cref="GameObject"/>.</param>
        /// <returns>The found or created child <see cref="GameObject"/>.</returns>
        public static GameObject GetOrAddChild(this GameObject gameObject, string name) {
            var child = gameObject.FindChild(name);
            if(child != null) return child;
            child = new GameObject(name);
            child.transform.parent = gameObject.transform;
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child;
        }
        
        /// <summary>
        /// This method is used to get or add a component to the passed <see cref="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">This is the <see cref="GameObject"/> that you want to get or add a component to.</param>
        /// <typeparam name="T">The type of the component that you want to get or add.</typeparam>
        /// <returns>The found or created <see cref="Component"/>.</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component {
            var c = gameObject.GetComponent<T>();
            return c != null ? c : gameObject.AddComponent<T>();
        }
        
        /// <summary>
        /// This method is used to destroy all of the <see cref="gameObject"/>s children.
        /// </summary>
        /// <param name="gameObject">The game object that you want to remove children from.</param>
        public static void DestroyChildren(this GameObject gameObject) {
            foreach(Transform child in gameObject.transform) UnityEngine.Object.Destroy(child.gameObject);
        }

        public static void DestroyChildrenEditMode(this GameObject gameObject) {
            if(Application.isEditor) gameObject.DestroyChildrenImmediate();
            else gameObject.DestroyChildren();
        }

        /// <summary>
        /// This method is used to destroy all of the <see cref="gameObject"/>s children immediately.
        /// </summary>
        /// <param name="gameObject">The game object that you want to remove children from.</param>
        public static void DestroyChildrenImmediate(this GameObject gameObject) {
            foreach(Transform child in gameObject.transform) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        /// <summary>
        /// This method is used to destroy a GameObject's components of the given type.
        /// </summary>
        /// <param name="gameObject">The game object you want to destroy components on.</param>
        /// <typeparam name="T">The type of component that you want to destroy.</typeparam>
        public static void DestroyComponents<T>(this GameObject gameObject) where T : Component {
            foreach(var component in gameObject.GetComponents<T>()) {
                if(Application.isEditor)UnityEngine.Object.DestroyImmediate(component);
                else UnityEngine.Object.Destroy(component);
            }
        }
    }
    
}