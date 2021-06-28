/* object.c - Object manipulation opcodes
 *	Copyright (c) 1995-1997 Stefan Jokisch
 *
 * This file is part of Frotz.
 *
 * Frotz is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * Frotz is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA
 */

using zbyte = System.Byte;
using zword = System.UInt16;

namespace Frotz.Generic
{
    internal static class CObject
    {
        internal static zword MAX_OBJECT = 2000;

        internal static zword O1_PARENT = 4;
        internal static zword O1_SIBLING = 5;
        internal static zword O1_CHILD = 6;
        internal static zword O1_PROPERTY_OFFSET = 7;
        internal static zword O1_SIZE = 9;

        internal static zword O4_PARENT = 6;
        internal static zword O4_SIBLING = 8;
        internal static zword O4_CHILD = 10;
        internal static zword O4_PROPERTY_OFFSET = 12;
        internal static zword O4_SIZE = 14;

        /*
         * object_address
         *
         * Calculate the address of an object.
         *
         */
        internal static zword ObjectAddress(zword obj)
        {
            /* Check object number */

            if (obj > ((Main.h_version <= ZMachine.V3) ? 255 : MAX_OBJECT))
            {
                Text.PrintString("@Attempt to address illegal object ");
                Text.PrintNum(obj);
                Text.PrintString(".  This is normally fatal.");
                Buffer.NewLine();
                Err.RuntimeError(ErrorCodes.ERR_ILL_OBJ);
            }

            /* Return object address */

            if (Main.h_version <= ZMachine.V3)
                return (zword)(Main.h_objects + ((obj - 1) * O1_SIZE + 62));
            else
                return (zword)(Main.h_objects + ((obj - 1) * O4_SIZE + 126));

        }/* object_address */

        /*
         * object_name
         *
         * Return the address of the given object's name.
         *
         */
        internal static zword ObjectName(zword object_var)
        {
            zword obj_addr = ObjectAddress(object_var);

            /* The object name address is found at the start of the properties */

            if (Main.h_version <= ZMachine.V3)
                obj_addr += O1_PROPERTY_OFFSET;
            else
                obj_addr += O4_PROPERTY_OFFSET;

            FastMem.LowWord(obj_addr, out zword name_addr);

            return name_addr;
        }/* object_name */

        /*
         * first_property
         *
         * Calculate the start address of the property list associated with
         * an object.
         *
         */
        private static zword FirstProperty(zword obj)
        {
            /* Fetch address of object name */

            zword prop_addr = ObjectName(obj);

            /* Get length of object name */

            FastMem.LowByte(prop_addr, out zbyte size);

            /* Add name length to pointer */

            return (zword)(prop_addr + 1 + 2 * size);

        }/* first_property */

        /*
         * next_property
         *
         * Calculate the address of the next property in a property list.
         *
         */
        private static zword NextProperty(zword prop_addr)
        {

            /* Load the current property id */

            FastMem.LowByte(prop_addr, out zbyte value);
            prop_addr++;

            /* Calculate the length of this property */

            if (Main.h_version <= ZMachine.V3)
            {
                value >>= 5;
            }
            else if (!((value & 0x80) > 0))
            {
                value >>= 6;
            }
            else
            {

                FastMem.LowByte(prop_addr, out value);
                value &= 0x3f;

                if (value == 0) value = 64;	/* demanded by Spec 1.0 */
            }

            /* Add property length to current property pointer */

            return (zword)(prop_addr + value + 1);

        }/* next_property */

        /*
         * unlink_object
         *
         * Unlink an object from its parent and siblings.
         *
         */
        private static void UnlinkObject(zword object_var)
        {
            zword parent_addr;
            zword sibling_addr;

            if (object_var == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_REMOVE_OBJECT_0);
                return;
            }

            zword obj_addr = ObjectAddress(object_var);

            if (Main.h_version <= ZMachine.V3)
            {

                zbyte zero = 0;

                /* Get parent of object, and return if no parent */

                obj_addr += O1_PARENT;
                FastMem.LowByte(obj_addr, out byte parent);
                if (parent == 0)
                    return;

                /* Get (older) sibling of object and set both parent and sibling
                   pointers to 0 */

                FastMem.SetByte(obj_addr, zero);
                obj_addr += (zword)(O1_SIBLING - O1_PARENT);
                FastMem.LowByte(obj_addr, out byte older_sibling);
                FastMem.SetByte(obj_addr, zero);

                /* Get first child of parent (the youngest sibling of the object) */

                parent_addr = (zword)(ObjectAddress(parent) + O1_CHILD);
                FastMem.LowByte(parent_addr, out byte younger_sibling);

                /* Remove object from the list of siblings */

                if (younger_sibling == object_var)
                {
                    FastMem.SetByte(parent_addr, older_sibling);
                }
                else
                {
                    do
                    {
                        sibling_addr = (zword)(ObjectAddress(younger_sibling) + O1_SIBLING);
                        FastMem.LowByte(sibling_addr, out younger_sibling);
                    } while (younger_sibling != object_var);
                    FastMem.SetByte(sibling_addr, older_sibling);
                }

            }
            else
            {

                zword zero = 0;

                /* Get parent of object, and return if no parent */

                obj_addr += O4_PARENT;
                FastMem.LowWord(obj_addr, out zword parent);
                if (parent == 0)
                    return;

                /* Get (older) sibling of object and set both parent and sibling
                   pointers to 0 */

                FastMem.SetWord(obj_addr, zero);
                obj_addr += (zword)(O4_SIBLING - O4_PARENT);
                FastMem.LowWord(obj_addr, out ushort older_sibling);
                FastMem.SetWord(obj_addr, zero);

                /* Get first child of parent (the youngest sibling of the object) */

                parent_addr = (zword)(ObjectAddress(parent) + O4_CHILD);
                FastMem.LowWord(parent_addr, out ushort younger_sibling);

                /* Remove object from the list of siblings */

                if (younger_sibling == object_var)
                {
                    FastMem.SetWord(parent_addr, older_sibling);
                }
                else
                {
                    do
                    {
                        sibling_addr = (zword)(ObjectAddress(younger_sibling) + O4_SIBLING);
                        FastMem.LowWord(sibling_addr, out younger_sibling);
                    } while (younger_sibling != object_var);
                    FastMem.SetWord(sibling_addr, older_sibling);
                }

            }

        }/* unlink_object */

        /*
         * z_clear_attr, clear an object attribute.
         *
         *	Process.zargs[0] = object
         *	Process.zargs[1] = number of attribute to be cleared
         *
         */
        internal static void ZClearAttr()
        {
            zword obj_addr;

            if (Main.StoryId == Story.SHERLOCK)
            {
                if (Process.zargs[1] == 48)
                    return;
            }

            if (Process.zargs[1] > ((Main.h_version <= ZMachine.V3) ? 31 : 47))
                Err.RuntimeError(ErrorCodes.ERR_ILL_ATTR);

            /* If we are monitoring attribute assignment display a short note */

            if (Main.option_attribute_assignment == true)
            {
                Stream.StreamMssgOn();
                Text.PrintString("@clear_attr ");
                Text.PrintObject(Process.zargs[0]);
                Text.PrintString(" ");
                Text.PrintNum(Process.zargs[1]);
                Stream.StreamMssgOff();
            }

            if (Process.zargs[0] == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_CLEAR_ATTR_0);
                return;
            }

            /* Get attribute address */

            obj_addr = (zword)(ObjectAddress(Process.zargs[0]) + Process.zargs[1] / 8);

            /* Clear attribute bit */

            FastMem.LowByte(obj_addr, out zbyte value);
            value &= (zbyte)(~(0x80 >> (Process.zargs[1] & 7)));
            FastMem.SetByte(obj_addr, value);

        }/* z_clear_attr */

        /*
         * z_jin, branch if the first object is inside the second.
         *
         *	Process.zargs[0] = first object
         *	Process.zargs[1] = second object
         *
         */
        internal static void ZJin()
        {
            zword obj_addr;

            /* If we are monitoring object locating display a short note */

            if (Main.option_object_locating)
            {
                Stream.StreamMssgOn();
                Text.PrintString("@jin ");
                Text.PrintObject(Process.zargs[0]);
                Text.PrintString(" ");
                Text.PrintObject(Process.zargs[1]);
                Stream.StreamMssgOff();
            }

            if (Process.zargs[0] == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_JIN_0);
                Process.Branch(0 == Process.zargs[1]);
                return;
            }

            obj_addr = ObjectAddress(Process.zargs[0]);

            if (Main.h_version <= ZMachine.V3)
            {
                /* Get parent id from object */

                obj_addr += O1_PARENT;
                FastMem.LowByte(obj_addr, out zbyte parent);

                /* Branch if the parent is obj2 */
                Process.Branch(parent == Process.zargs[1]);
            }
            else
            {
                /* Get parent id from object */

                obj_addr += O4_PARENT;
                FastMem.LowWord(obj_addr, out zword parent);

                /* Branch if the parent is obj2 */

                Process.Branch(parent == Process.zargs[1]);
            }

        }/* z_jin */

        /*
         * z_get_child, store the child of an object.
         *
         *	Process.zargs[0] = object
         *
         */

        internal static void ZGetChild()
        {
            zword obj_addr;

            /* If we are monitoring object locating display a short note */

            if (Main.option_object_locating)
            {
                Stream.StreamMssgOn();
                Text.PrintString("@get_child ");
                Text.PrintObject(Process.zargs[0]);
                Stream.StreamMssgOff();
            }

            if (Process.zargs[0] == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_GET_CHILD_0);
                Process.Store(0);
                Process.Branch(false);
                return;
            }

            obj_addr = ObjectAddress(Process.zargs[0]);

            if (Main.h_version <= ZMachine.V3)
            {
                /* Get child id from object */
                obj_addr += O1_CHILD;
                FastMem.LowByte(obj_addr, out zbyte child);

                /* Store child id and branch */
                Process.Store(child);
                Process.Branch(child > 0);

            }
            else
            {
                /* Get child id from object */
                obj_addr += O4_CHILD;
                FastMem.LowWord(obj_addr, out zword child);

                /* Store child id and branch */
                Process.Store(child);
                Process.Branch(child > 0);

            }

        }/* z_get_child */

        /*
         * z_get_next_prop, store the number of the first or next property.
         *
         *	Process.zargs[0] = object
         *	Process.zargs[1] = address of current property (0 gets the first property)
         *
         */
        internal static void ZGetNextProp()
        {
            zbyte value;

            if (Process.zargs[0] == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_GET_NEXT_PROP_0);
                Process.Store(0);
                return;
            }

            /* Property id is in bottom five (six) bits */
            zbyte mask = (zbyte)((Main.h_version <= ZMachine.V3) ? 0x1f : 0x3f);

            /* Load address of first property */
            zword prop_addr = FirstProperty(Process.zargs[0]);

            if (Process.zargs[1] != 0)
            {
                /* Scan down the property list */

                do
                {
                    FastMem.LowByte(prop_addr, out value);
                    prop_addr = NextProperty(prop_addr);
                } while ((value & mask) > Process.zargs[1]);

                /* Exit if the property does not exist */

                if ((value & mask) != Process.zargs[1])
                    Err.RuntimeError(ErrorCodes.ERR_NO_PROP);

            }

            /* Return the property id */

            FastMem.LowByte(prop_addr, out value);
            Process.Store((zword)(value & mask));

        }/* z_get_next_prop */

        /*
         * z_get_parent, store the parent of an object.
         *
         *	Process.zargs[0] = object
         *
         */
        internal static void ZGetParent()
        {
            zword obj_addr;

            /* If we are monitoring object locating display a short note */

            if (Main.option_object_locating)
            {
                Stream.StreamMssgOn();
                Text.PrintString("@get_parent ");
                Text.PrintObject(Process.zargs[0]);
                Stream.StreamMssgOff();
            }

            if (Process.zargs[0] == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_GET_PARENT_0);
                Process.Store(0);
                return;
            }

            obj_addr = ObjectAddress(Process.zargs[0]);

            if (Main.h_version <= ZMachine.V3)
            {
                /* Get parent id from object */

                obj_addr += O1_PARENT;
                FastMem.LowByte(obj_addr, out zbyte parent);

                /* Store parent */

                Process.Store(parent);
            }
            else
            {
                /* Get parent id from object */

                obj_addr += O4_PARENT;
                FastMem.LowWord(obj_addr, out zword parent);

                /* Store parent */

                Process.Store(parent);
            }

        }/* z_get_parent */

        /*
         * z_get_prop, store the value of an object property.
         *
         *	Process.zargs[0] = object
         *	Process.zargs[1] = number of property to be examined
         *
         */

        internal static void ZGetProp()
        {
            zword prop_addr;
            zword wprop_val;
            zbyte value;
            zbyte mask;

            if (Process.zargs[0] == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_GET_PROP_0);
                Process.Store(0);
                return;
            }

            /* Property id is in bottom five (six) bits */
            mask = (zbyte)((Main.h_version <= ZMachine.V3) ? 0x1f : 0x3f);

            /* Load address of first property */
            prop_addr = FirstProperty(Process.zargs[0]);

            /* Scan down the property list */
            for (; ; )
            {
                FastMem.LowByte(prop_addr, out value);
                if ((value & mask) <= Process.zargs[1])
                    break;
                prop_addr = NextProperty(prop_addr);
            }

            if ((value & mask) == Process.zargs[1])
            {	/* property found */

                /* Load property (byte or word sized) */
                prop_addr++;

                if ((Main.h_version <= ZMachine.V3 && !((value & 0xe0) > 0)) || (Main.h_version >= ZMachine.V4 && !((value & 0xc0) > 0)))
                {
                    FastMem.LowByte(prop_addr, out zbyte bprop_val);
                    wprop_val = bprop_val;
                }
                else
                {
                    FastMem.LowWord(prop_addr, out wprop_val);
                }
            }
            else
            {	/* property not found */

                /* Load default value */

                prop_addr = (zword)(Main.h_objects + 2 * (Process.zargs[1] - 1));
                FastMem.LowWord(prop_addr, out wprop_val);

            }

            /* Store the property value */
            Process.Store(wprop_val);

        }/* z_get_prop */

        /*
         * z_get_prop_addr, store the address of an object property.
         *
         *	Process.zargs[0] = object
         *	Process.zargs[1] = number of property to be examined
         *
         */

        internal static void ZGetPropAddr()
        {
            zword prop_addr;
            zbyte value;
            zbyte mask;

            if (Process.zargs[0] == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_GET_PROP_ADDR_0);
                Process.Store(0);
                return;
            }

            if (Main.StoryId == Story.BEYOND_ZORK && Process.zargs[0] > MAX_OBJECT)
            {
                Process.Store(0);
                return;
            }

            /* Property id is in bottom five (six) bits */

            mask = (zbyte)((Main.h_version <= ZMachine.V3) ? 0x1f : 0x3f);

            /* Load address of first property */
            prop_addr = FirstProperty(Process.zargs[0]);

            /* Scan down the property list */

            for (; ; )
            {
                FastMem.LowByte(prop_addr, out value);
                if ((value & mask) <= Process.zargs[1])
                    break;
                prop_addr = NextProperty(prop_addr);
            }

            /* Calculate the property address or return zero */

            if ((value & mask) == Process.zargs[1])
            {
                if (Main.h_version >= ZMachine.V4 && (value & 0x80) > 0)
                    prop_addr++;
                Process.Store((zword)(prop_addr + 1));
            }
            else
            {
                Process.Store(0);
            }
        }/* z_get_prop_addr */

        /*
         * z_get_prop_len, store the length of an object property.
         *
         * 	Process.zargs[0] = address of property to be examined
         *
         */

        internal static void ZGetPropLen()
        {
            zword addr;

            /* Back up the property pointer to the property id */

            addr = (zword)(Process.zargs[0] - 1);
            FastMem.LowByte(addr, out byte value);

            /* Calculate length of property */

            if (Main.h_version <= ZMachine.V3)
            {
                value = (zbyte)((value >> 5) + 1);
            }
            else if (!((value & 0x80) > 0))
            {
                value = (zbyte)((value >> 6) + 1);
            }
            else
            {
                value &= 0x3f;

                if (value == 0) value = 64;	/* demanded by Spec 1.0 */
            }

            /* Store length of property */
            Process.Store(value);

        }/* z_get_prop_len */

        /*
         * z_get_sibling, store the sibling of an object.
         *
         *	Process.zargs[0] = object
         *
         */

        internal static void ZGetSibling()
        {
            zword obj_addr;

            if (Process.zargs[0] == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_GET_SIBLING_0);
                Process.Store(0);
                Process.Branch(false);
                return;
            }

            obj_addr = ObjectAddress(Process.zargs[0]);

            if (Main.h_version <= ZMachine.V3)
            {
                /* Get sibling id from object */

                obj_addr += O1_SIBLING;
                FastMem.LowByte(obj_addr, out byte sibling);

                /* Store sibling and branch */

                Process.Store(sibling);
                Process.Branch(sibling > 0); // TODO I'm not sure about this logic Process.branch (sibling);
                // I think it means if the sibling isn't zero, jump..
            }
            else
            {
                /* Get sibling id from object */
                obj_addr += O4_SIBLING;
                FastMem.LowWord(obj_addr, out ushort sibling);

                /* Store sibling and branch */
                Process.Store(sibling);
                Process.Branch(sibling > 0);
            }

        }/* z_get_sibling */

        /*
         * z_insert_obj, make an object the first child of another object.
         *
         *	Process.zargs[0] = object to be moved
         *	Process.zargs[1] = destination object
         *
         */

        internal static void ZInsertObj()
        {
            zword obj1 = Process.zargs[0];
            zword obj2 = Process.zargs[1];
            zword obj1_addr;
            zword obj2_addr;

            /* If we are monitoring object movements display a short note */

            if (Main.option_object_movement == true)
            {
                Stream.StreamMssgOn();
                Text.PrintString("@move_obj ");
                Text.PrintObject(obj1);
                Text.PrintString(" ");
                Text.PrintObject(obj2);
                Stream.StreamMssgOff();
            }

            if (obj1 == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_MOVE_OBJECT_0);
                return;
            }

            if (obj2 == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_MOVE_OBJECT_TO_0);
                return;
            }

            /* Get addresses of both objects */
            obj1_addr = ObjectAddress(obj1);
            obj2_addr = ObjectAddress(obj2);

            /* Remove object 1 from current parent */
            UnlinkObject(obj1);

            /* Make object 1 first child of object 2 */
            if (Main.h_version <= ZMachine.V3)
            {
                obj1_addr += O1_PARENT;
                FastMem.SetByte(obj1_addr, (zbyte)obj2);
                obj2_addr += O1_CHILD;
                FastMem.LowByte(obj2_addr, out byte child);
                FastMem.SetByte(obj2_addr, (zbyte)obj1);
                obj1_addr += (zword)(O1_SIBLING - O1_PARENT);
                FastMem.SetByte(obj1_addr, child);
            }
            else
            {
                obj1_addr += O4_PARENT;
                FastMem.SetWord(obj1_addr, obj2);
                obj2_addr += O4_CHILD;
                FastMem.LowWord(obj2_addr, out ushort child);
                FastMem.SetWord(obj2_addr, obj1);
                obj1_addr += (zword)(O4_SIBLING - O4_PARENT);
                FastMem.SetWord(obj1_addr, child);
            }

        }/* z_insert_obj */

        /*
         * z_put_prop, set the value of an object property.
         *
         *	Process.zargs[0] = object
         *	Process.zargs[1] = number of property to set
         *	Process.zargs[2] = value to set property to
         *
         */
        internal static void ZPutProp()
        {
            zword prop_addr;
            zbyte value;
            zbyte mask;

            if (Process.zargs[0] == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_PUT_PROP_0);
                return;
            }

            /* Property id is in bottom five or six bits */
            mask = (zbyte)((Main.h_version <= ZMachine.V3) ? 0x1f : 0x3f);

            /* Load address of first property */
            prop_addr = FirstProperty(Process.zargs[0]);

            /* Scan down the property list */
            for (; ; )
            {
                FastMem.LowByte(prop_addr, out value);
                if ((value & mask) <= Process.zargs[1])
                    break;
                prop_addr = NextProperty(prop_addr);
            }

            /* Exit if the property does not exist */

            if ((value & mask) != Process.zargs[1])
                Err.RuntimeError(ErrorCodes.ERR_NO_PROP);

            /* Store the new property value (byte or word sized) */
            prop_addr++;

            if ((Main.h_version <= ZMachine.V3 && !((value & 0xe0) > 0)) || (Main.h_version >= ZMachine.V4 && !((value & 0xc0) > 0)))
            {
                zbyte v = (zbyte)Process.zargs[2];
                FastMem.SetByte(prop_addr, v);
            }
            else
            {
                zword v = Process.zargs[2];
                FastMem.SetWord(prop_addr, v);
            }

        }/* z_put_prop */

        /*
         * z_remove_obj, unlink an object from its parent and siblings.
         *
         *	Process.zargs[0] = object
         *
         */
        internal static void ZRemoveObj()
        {
            /* If we are monitoring object movements display a short note */

            if (Main.option_object_movement == true)
            {
                Stream.StreamMssgOn();
                Text.PrintString("@remove_obj ");
                Text.PrintObject(Process.zargs[0]);
                Stream.StreamMssgOff();
            }

            /* Call unlink_object to do the job */

            UnlinkObject(Process.zargs[0]);

        }/* z_remove_obj */

        /*
         * z_set_attr, set an object attribute.
         *
         *	Process.zargs[0] = object
         *	Process.zargs[1] = number of attribute to set
         *
         */
        internal static void ZSetAttr()
        {
            zword obj_addr;

            if (Main.StoryId == Story.SHERLOCK)
            {
                if (Process.zargs[1] == 48)
                    return;
            }

            if (Process.zargs[1] > ((Main.h_version <= ZMachine.V3) ? 31 : 47))
                Err.RuntimeError(ErrorCodes.ERR_ILL_ATTR);

            /* If we are monitoring attribute assignment display a short note */

            if (Main.option_attribute_assignment == true)
            {
                Stream.StreamMssgOn();
                Text.PrintString("@set_attr ");
                Text.PrintObject(Process.zargs[0]);
                Text.PrintString(" ");
                Text.PrintNum(Process.zargs[1]);
                Stream.StreamMssgOff();
            }

            if (Process.zargs[0] == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_SET_ATTR_0);
                return;
            }

            /* Get attribute address */
            obj_addr = (zword)(ObjectAddress(Process.zargs[0]) + Process.zargs[1] / 8);

            /* Load attribute byte */
            FastMem.LowByte(obj_addr, out zbyte value);

            /* Set attribute bit */
            value |= (zbyte)(0x80 >> (Process.zargs[1] & 7));

            /* Store attribute byte */
            FastMem.SetByte(obj_addr, value);

        }/* z_set_attr */

        /*
         * z_test_attr, branch if an object attribute is set.
         *
         *	Process.zargs[0] = object
         *	Process.zargs[1] = number of attribute to test
         *
         */
        internal static void ZTestAttr()
        {
            zword obj_addr;

            if (Process.zargs[1] > ((Main.h_version <= ZMachine.V3) ? 31 : 47))
                Err.RuntimeError(ErrorCodes.ERR_ILL_ATTR);

            /* If we are monitoring attribute testing display a short note */

            if (Main.option_attribute_testing == true)
            {
                Stream.StreamMssgOn();
                Text.PrintString("@test_attr ");
                Text.PrintObject(Process.zargs[0]);
                Text.PrintString(" ");
                Text.PrintNum(Process.zargs[1]);
                Stream.StreamMssgOff();
            }

            if (Process.zargs[0] == 0)
            {
                Err.RuntimeError(ErrorCodes.ERR_TEST_ATTR_0);
                Process.Branch(false);
                return;
            }

            /* Get attribute address */
            obj_addr = (zword)(ObjectAddress(Process.zargs[0]) + Process.zargs[1] / 8);

            /* Load attribute byte */
            FastMem.LowByte(obj_addr, out byte value);

            /* Test attribute */
            Process.Branch((value & (0x80 >> (Process.zargs[1] & 7))) > 0);
        }/* z_test_attr */
    }
}