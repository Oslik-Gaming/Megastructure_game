using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Progression;
using Progression.Actions;
using Progression.Conditions;

namespace ProgressionEditor
{
    [CustomPropertyDrawer(typeof(ProgressionStep))]
    public class ProgressionStepDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            string key = property.propertyPath;
            bool isExpanded = foldoutStates.ContainsKey(key) && foldoutStates[key];

            if (!isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight + 4; // Foldout
            height += EditorGUIUtility.singleLineHeight + 2; // stepId
            height += EditorGUIUtility.singleLineHeight + 2; // taskText
            height += EditorGUIUtility.singleLineHeight + 4; // Entry Actions label
            
            var entryActions = property.FindPropertyRelative("entryActions");
            if (entryActions != null && entryActions.isExpanded)
            {
                height += EditorGUIUtility.singleLineHeight + 2; // Size field
                for (int i = 0; i < entryActions.arraySize; i++)
                {
                    var element = entryActions.GetArrayElementAtIndex(i);
                    height += EditorGUI.GetPropertyHeight(element, true) + 4;
                }
                height += EditorGUIUtility.singleLineHeight + 4; // Add button
            }

            height += EditorGUIUtility.singleLineHeight + 4; // Conditions label
            var conditions = property.FindPropertyRelative("conditions");
            if (conditions != null && conditions.isExpanded)
            {
                height += EditorGUIUtility.singleLineHeight + 2; // Size field
                for (int i = 0; i < conditions.arraySize; i++)
                {
                    var element = conditions.GetArrayElementAtIndex(i);
                    height += EditorGUI.GetPropertyHeight(element, true) + 4;
                }
                height += EditorGUIUtility.singleLineHeight + 4; // Add button
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string key = property.propertyPath;
            if (!foldoutStates.ContainsKey(key))
                foldoutStates[key] = false;

            EditorGUI.BeginProperty(position, label, property);

            // Main foldout
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            foldoutStates[key] = EditorGUI.Foldout(foldoutRect, foldoutStates[key], label, true);

            if (foldoutStates[key])
            {
                EditorGUI.indentLevel++;
                float yPos = position.y + EditorGUIUtility.singleLineHeight + 4;

                // Step ID
                var stepId = property.FindPropertyRelative("stepId");
                Rect stepIdRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(stepIdRect, stepId);
                yPos += EditorGUIUtility.singleLineHeight + 2;

                // Task Text
                var taskText = property.FindPropertyRelative("taskText");
                Rect taskTextRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(taskTextRect, taskText);
                yPos += EditorGUIUtility.singleLineHeight + 2;

                // Entry Actions
                yPos = DrawSerializeReferenceList(position, property, "entryActions", "Entry Actions", yPos, GetActionTypes());

                // Conditions
                yPos = DrawSerializeReferenceList(position, property, "conditions", "Conditions", yPos, GetConditionTypes());

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private float DrawSerializeReferenceList(Rect position, SerializedProperty property, string fieldName, string label, float yPos, Type[] availableTypes)
        {
            var list = property.FindPropertyRelative(fieldName);
            if (list == null) return yPos;

            Rect labelRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            list.isExpanded = EditorGUI.Foldout(labelRect, list.isExpanded, label, true);
            yPos += EditorGUIUtility.singleLineHeight + 4;

            if (list.isExpanded)
            {
                EditorGUI.indentLevel++;

                // Size field
                Rect sizeRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                int newSize = EditorGUI.IntField(sizeRect, "Size", list.arraySize);
                if (newSize != list.arraySize)
                {
                    list.arraySize = newSize;
                }
                yPos += EditorGUIUtility.singleLineHeight + 2;

                // Elements
                for (int i = 0; i < list.arraySize; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    
                    Rect elementRect = new Rect(position.x, yPos, position.width - 60, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(elementRect, element, new GUIContent($"Element {i}"), true);

                    // Delete button
                    Rect deleteRect = new Rect(position.x + position.width - 55, yPos, 50, EditorGUIUtility.singleLineHeight);
                    if (GUI.Button(deleteRect, "Delete"))
                    {
                        list.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    yPos += EditorGUI.GetPropertyHeight(element, true) + 4;
                }

                // Add button with dropdown
                Rect addRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(addRect, $"Add {label}"))
                {
                    ShowAddMenu(list, availableTypes, property);
                }
                yPos += EditorGUIUtility.singleLineHeight + 4;

                EditorGUI.indentLevel--;
            }

            return yPos;
        }

        private void ShowAddMenu(SerializedProperty list, Type[] availableTypes, SerializedProperty rootProperty)
        {
            GenericMenu menu = new GenericMenu();

            foreach (var type in availableTypes)
            {
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    int index = list.arraySize;
                    list.arraySize++;
                    var newElement = list.GetArrayElementAtIndex(index);
                    newElement.managedReferenceValue = Activator.CreateInstance(type);
                    rootProperty.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        private Type[] GetActionTypes()
        {
            return new Type[]
            {
                typeof(ShowTaskAction),
                typeof(SpawnToolAction),
                typeof(SwitchLocationTrackingAction),
                typeof(ShowDirectionArrowAction),
                typeof(HideDirectionArrowAction),
                typeof(OpenDoorAction),
                typeof(PlayCutsceneAction),
                typeof(MarkGameCompletedAction),
                typeof(LoadSceneAction)
            };
        }

        private Type[] GetConditionTypes()
        {
            return new Type[]
            {
                typeof(ToolPickedCondition),
                typeof(LocationCleanedCondition),
                typeof(ObjectDestroyedCondition),
                typeof(ZoneEnteredCondition),
                typeof(ButtonPressedCondition)
            };
        }
    }
}
