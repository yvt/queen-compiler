using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin.CompilerServices
{
    public delegate void Action0();
    public delegate void Action1<T>(T param);
    public delegate void Action2<T1,T2>(T1 p1, T2 p2);
    public delegate void Action3<T1,T2,T3>(T1 p1, T2 p2, T3 p3);
    public delegate void Action4<T1,T2,T3,T4>(T1 p1, T2 p2, T3 p3, T4 p4);
    public delegate void Action5<T1, T2, T3, T4, T5>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5);
    public delegate void Action6<T1, T2, T3, T4, T5, T6>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6);
    public delegate void Action7<T1, T2, T3, T4, T5, T6, T7>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7);
    public delegate void Action8<T1, T2, T3, T4, T5, T6, T7, T8>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8);
    public delegate R Func0<R>();
    public delegate R Func1<T, R>(T param);
    public delegate R Func2<T1, T2, R>(T1 p1, T2 p2);
    public delegate R Func3<T1, T2, T3, R>(T1 p1, T2 p2, T3 p3);
    public delegate R Func4<T1, T2, T3, T4, R>(T1 p1, T2 p2, T3 p3, T4 p4);
    public delegate R Func5<T1, T2, T3, T4, T5, R>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5);
    public delegate R Func6<T1, T2, T3, T4, T5, T6, R>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6);
    public delegate R Func7<T1, T2, T3, T4, T5, T6, T7, R>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7);
    public delegate R Func8<T1, T2, T3, T4, T5, T6, T7, T8, R>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8);
}
