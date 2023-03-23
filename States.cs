using System;
using System.Collections.Generic;
using System.Text;

namespace CS_Compiler_For_FreePascal
{
    enum States : int
    {
        error = -1,
        eof = 0,            // конец файла
        delimit = 1,        // разделители
        opersign = 2,       // знаки операций
        keyword = 3,        // ключевые слова
        identifier = 4,     // идентификаторы
        liter = 5,          // символьные литералы
        strliter = 6,       // строковые литералы
        integer = 7,        // целые числа
        real = 8,           // вещественные числа
    };
}
