/*
 * informinfo V7/3
 *
 * Inform 6 specific routines.
 * 
 * Matthew T. Russotto 7 February 1998 russotto@pond.com
 *
 */
namespace ZTools;

using zword_t = System.UInt16;

internal static class InfInfo
{
    internal static void ConfigureInformTables(
        ulong obj_data_end, /* everything follows from this */
        out ushort inform_version,
        out ulong class_numbers_base,
        out ulong class_numbers_end,
        out ulong property_names_base,
        out ulong property_names_end,
        out ulong attr_names_base,
        out ulong attr_names_end)
    {
        ulong address;
        zword_t num_properties;

        attr_names_base = attr_names_end = 0;
        property_names_base = property_names_end = 0;
        class_numbers_base = class_numbers_end = 0;

        inform_version = 0;

        var header = txio.header;
        if (header == null)
            throw new InvalidOperationException("txio header was not initialized");

        if (header.serial[0] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[1] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[2] is >= (byte)'0' and <= (byte)'1' &&
            header.serial[3] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[4] is >= (byte)'0' and <= (byte)'3' &&
            header.serial[5] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[0] != '8')
        {
            if (header.name[4] >= '6')
            {
                inform_version = (ushort)((header.name[4] - '0') * 100 + (header.name[6] - '0') * 10 + (header.name[7] - '0'));
                address = class_numbers_base = obj_data_end + 1;
                while (txio.ReadDataWord(ref address) > 0) /* do nothing */;
                class_numbers_end = address - 1;
                property_names_base = address;
                num_properties = (zword_t)(txio.ReadDataWord(ref address) - 1);
                address += (ulong)(num_properties * sizeof(zword_t));
                property_names_end = address - 1;
                if (inform_version >= TxH.INFORM_610)
                {
                    attr_names_base = address;
                    address += (48 * sizeof(zword_t));
                    attr_names_end = address - 1;
                    /* then come the action names, the individual property values, the dynamic arrays, etc */
                }
            }
        }
        else
        {
            inform_version = 0;
        }

        txio.TxPrintf("Inform Version: {0}\n", inform_version);
    }

    internal static int PrintInformAttributeName(ulong attr_names_base, int attr_no)
    {
        ulong address = attr_names_base + (ulong)attr_no * 2;
        address = txio.ReadDataWord(ref address);
        if (address == 0)
            return 0;

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        address = address * txio.code_scaler + txio.story_scaler * txio.header.strings_offset;
        txio.DecodeText(ref address);
        return 1;
    }

    //#ifdef __STDC__
    //int print_inform_property_name(ulong prop_names_base, int prop_no)
    //#else
    //int print_inform_property_name(prop_names_base, prop_no)
    //ulong prop_names_base;
    //int prop_no;
    //#endif
    //{
    //    ulong address;

    //    address = prop_names_base + prop_no * 2;
    //    address = (ulong) txio.read_data_word (&address);
    //    if (address == 0)
    //        return 0;
    //    address = address * code_scaler + (ulong) story_scaler * header.strings_offset;
    //    txio.decode_text(&address);
    //    return 1;
    //}

    internal static int PrintInformActionName(ulong action_names_base, int action_no)
    {
        ulong address = action_names_base + (ulong)action_no * 2;
        address = txio.ReadDataWord(ref address);
        if (address == 0)
            return 0;

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        address = address * txio.code_scaler + txio.story_scaler * txio.header.strings_offset;
        txio.DecodeText(ref address);
        return 1;
    }
}
